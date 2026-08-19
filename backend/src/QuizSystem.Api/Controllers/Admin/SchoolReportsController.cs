using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuizSystem.Infrastructure.Persistence;
using System.Security.Claims;
using System.Text;

namespace QuizSystem.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/school/reports")]
[Authorize(Policy = "AdminOnly")]
public class SchoolReportsController : ControllerBase
{
    private const string ArabicFont = "Tajawal";
    private readonly AppDbContext _db;

    public SchoolReportsController(AppDbContext db)
    {
        _db = db;
    }

    private Guid? CurrentInstitutionId()
    {
        var raw = User.FindFirstValue("institutionId") ?? User.FindFirstValue("InstitutionId");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private async Task<string> InstitutionNameAsync(CancellationToken ct)
    {
        var institutionId = CurrentInstitutionId();
        if (institutionId is null) return "المؤسسة التعليمية";
        return await _db.Institutions
            .Where(x => x.Id == institutionId.Value)
            .Select(x => x.Name)
            .FirstOrDefaultAsync(ct) ?? "المؤسسة التعليمية";
    }

    private IQueryable<QuizSystem.Domain.Entities.StudentProfile> ScopedStudents()
    {
        var institutionId = CurrentInstitutionId();
        var query = _db.Students.AsNoTracking();
        if (institutionId is null) return query;

        var studentIds = _db.Users
            .Where(u => u.InstitutionId == institutionId.Value && u.StudentProfileId != null)
            .Select(u => u.StudentProfileId!.Value);

        return query.Where(s => studentIds.Contains(s.Id));
    }

    [HttpGet("students/excel")]
    public async Task<IActionResult> ExportStudentsExcel(CancellationToken ct)
    {
        var institutionName = await InstitutionNameAsync(ct);
        var exportedAt = DateTime.Now;
        var rows = await ScopedStudents()
            .OrderBy(x => x.FullName)
            .Select(x => new
            {
                x.FullName,
                x.StudentCode,
                x.Grade,
                x.Branch,
                x.NationalId,
                x.Mobile,
                x.Nationality,
                x.ImagePath,
                x.IsActive
            })
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine($"Institution,{EscapeCsv(institutionName)}");
        sb.AppendLine($"Exported At,{exportedAt:yyyy-MM-dd HH:mm}");
        sb.AppendLine();
        sb.AppendLine("Full Name,Student Code,Grade,Branch,National ID,Mobile,Nationality,Image Path,Status");
        foreach (var x in rows)
        {
            sb.AppendLine(string.Join(',', new[]
            {
                EscapeCsv(x.FullName), EscapeCsv(x.StudentCode), EscapeCsv(x.Grade),
                EscapeCsv(x.Branch), EscapeCsv(x.NationalId), EscapeCsv(x.Mobile),
                EscapeCsv(x.Nationality), EscapeCsv(x.ImagePath), x.IsActive ? "Active" : "Inactive"
            }));
        }

        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray(),
            "text/csv", $"students-report-{DateTime.UtcNow:yyyyMMddHHmm}.csv");
    }

    [HttpGet("students/pdf")]
    public async Task<IActionResult> ExportStudentsPdf(CancellationToken ct)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var institutionName = await InstitutionNameAsync(ct);
        var exportedAt = DateTime.Now;
        var rows = await ScopedStudents()
            .OrderBy(x => x.FullName)
            .Select(x => new
            {
                x.FullName,
                x.StudentCode,
                x.Grade,
                x.Branch,
                x.NationalId,
                x.Mobile,
                x.Nationality,
                x.IsActive
            })
            .ToListAsync(ct);

        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(28);
                page.Size(PageSizes.A4.Landscape());
                page.DefaultTextStyle(x => x.FontFamily(ArabicFont).FontSize(10));
                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text(institutionName).Bold().FontSize(18);
                    col.Item().AlignCenter().Text($"تقرير الطلاب - تاريخ التصدير: {exportedAt:yyyy-MM-dd HH:mm}").FontSize(11);
                    col.Item().PaddingTop(8).LineHorizontal(1);
                });
                page.Content().PaddingTop(12).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn();
                        c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn();
                    });
                    string[] headers = { "الاسم", "الكود", "الصف", "الفرع", "الهوية", "الجوال", "الجنسية", "الحالة" };
                    foreach (var h in headers)
                        table.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text(h).Bold();
                    foreach (var x in rows)
                    {
                        table.Cell().Padding(4).Text(x.FullName ?? "-");
                        table.Cell().Padding(4).Text(x.StudentCode ?? "-");
                        table.Cell().Padding(4).Text(x.Grade ?? "-");
                        table.Cell().Padding(4).Text(x.Branch == "female" ? "بنات" : "بنين");
                        table.Cell().Padding(4).Text(x.NationalId ?? "-");
                        table.Cell().Padding(4).Text(x.Mobile ?? "-");
                        table.Cell().Padding(4).Text(x.Nationality ?? "-");
                        table.Cell().Padding(4).Text(x.IsActive ? "نشط" : "معطل");
                    }
                });
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("صفحة "); x.CurrentPageNumber(); x.Span(" من "); x.TotalPages();
                });
            });
        }).GeneratePdf();

        return File(bytes, "application/pdf", $"students-report-{DateTime.UtcNow:yyyyMMddHHmm}.pdf");
    }

    private static string EscapeCsv(string? value)
    {
        value ??= string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
