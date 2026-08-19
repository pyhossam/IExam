using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuizSystem.Domain.Entities;
using QuizSystem.Infrastructure.Persistence;
using System.Security.Claims;
using System.Text;

namespace QuizSystem.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/school")]
[Authorize(Policy = "AdminOnly")]
public class SchoolStudentsAndReportsController : ControllerBase
{
    private const string ArabicFont = "Tajawal";
    private readonly AppDbContext _db;

    public SchoolStudentsAndReportsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("nationalities")]
    public IActionResult GetNationalities()
    {
        var list = new[]
        {
            "سعودي", "مصري", "سوري", "أردني", "فلسطيني", "يمني", "سوداني",
            "باكستاني", "هندي", "بنغلاديشي", "فلبيني", "إندونيسي", "أخرى"
        };

        return Ok(list.Select(x => new { value = x, label = x }));
    }

    [HttpGet("students/details")]
    public async Task<IActionResult> GetInstitutionStudents(CancellationToken cancellationToken)
    {
        var institutionId = GetInstitutionId();

        var query = _db.Students.AsNoTracking();

        if (institutionId.HasValue)
        {
            query = query.Where(student => _db.Users.Any(user =>
                user.StudentProfileId == student.Id &&
                user.InstitutionId == institutionId.Value));
        }

        var rows = await query
            .OrderBy(x => x.FullName)
            .Select(x => new
            {
                x.Id,
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
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }

    [HttpGet("reports/{kind}/excel")]
    public async Task<IActionResult> ExportExcel(string kind, CancellationToken cancellationToken)
    {
        var institutionName = await GetInstitutionNameAsync(cancellationToken);
        var exportedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        var csv = new StringBuilder();

        csv.AppendLine($"Institution,{EscapeCsv(institutionName)}");
        csv.AppendLine($"Exported At,{EscapeCsv(exportedAt)}");
        csv.AppendLine();

        if (kind.Equals("students", StringComparison.OrdinalIgnoreCase))
        {
            csv.AppendLine("Full Name,Student Code,Grade,Branch,National ID,Mobile,Nationality,Status");
            foreach (var s in await BuildStudentsQuery().ToListAsync(cancellationToken))
            {
                csv.AppendLine(string.Join(',', new[]
                {
                    EscapeCsv(s.FullName), EscapeCsv(s.StudentCode), EscapeCsv(s.Grade), EscapeCsv(s.Branch),
                    EscapeCsv(s.NationalId), EscapeCsv(s.Mobile), EscapeCsv(s.Nationality), EscapeCsv(s.IsActive ? "Active" : "Inactive")
                }));
            }
        }
        else
        {
            csv.AppendLine("Report,Institution,Exported At");
            csv.AppendLine($"{EscapeCsv(kind)},{EscapeCsv(institutionName)},{EscapeCsv(exportedAt)}");
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"{kind}_report_{DateTime.Now:yyyyMMdd_HHmm}.csv");
    }

    [HttpGet("reports/{kind}/pdf")]
    public async Task<IActionResult> ExportPdf(string kind, CancellationToken cancellationToken)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var institutionName = await GetInstitutionNameAsync(cancellationToken);
        var exportedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        var students = kind.Equals("students", StringComparison.OrdinalIgnoreCase)
            ? await BuildStudentsQuery().ToListAsync(cancellationToken)
            : new List<StudentReportRow>();

        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4.Landscape());
                page.DefaultTextStyle(x => x.FontFamily(ArabicFont).FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text(institutionName).Bold().FontSize(18).AlignCenter();
                    col.Item().Text($"تاريخ التصدير: {exportedAt}").FontSize(10).AlignCenter();
                    col.Item().PaddingTop(8).LineHorizontal(1);
                });

                page.Content().PaddingVertical(15).Column(col =>
                {
                    col.Item().Text(GetReportTitle(kind)).Bold().FontSize(15);
                    col.Item().PaddingTop(10);

                    if (kind.Equals("students", StringComparison.OrdinalIgnoreCase))
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            HeaderCell(table, "الاسم");
                            HeaderCell(table, "الكود");
                            HeaderCell(table, "الصف");
                            HeaderCell(table, "الفرع");
                            HeaderCell(table, "رقم الهوية");
                            HeaderCell(table, "الجوال");
                            HeaderCell(table, "الجنسية");

                            foreach (var s in students)
                            {
                                BodyCell(table, s.FullName);
                                BodyCell(table, s.StudentCode);
                                BodyCell(table, s.Grade);
                                BodyCell(table, s.Branch);
                                BodyCell(table, s.NationalId);
                                BodyCell(table, s.Mobile);
                                BodyCell(table, s.Nationality);
                            }
                        });
                    }
                    else
                    {
                        col.Item().Text("هذا التقرير جاهز للتصدير، ولا توجد بيانات تفصيلية متاحة له حاليًا.");
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("صفحة ");
                    x.CurrentPageNumber();
                    x.Span(" من ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();

        return File(bytes, "application/pdf", $"{kind}_report_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
    }

    private IQueryable<StudentReportRow> BuildStudentsQuery()
    {
        var institutionId = GetInstitutionId();
        var query = _db.Students.AsNoTracking();

        if (institutionId.HasValue)
        {
            query = query.Where(student => _db.Users.Any(user =>
                user.StudentProfileId == student.Id &&
                user.InstitutionId == institutionId.Value));
        }

        return query.OrderBy(x => x.FullName).Select(x => new StudentReportRow
        {
            FullName = x.FullName,
            StudentCode = x.StudentCode,
            Grade = x.Grade,
            Branch = x.Branch ?? "",
            NationalId = x.NationalId ?? "",
            Mobile = x.Mobile ?? "",
            Nationality = x.Nationality ?? "",
            IsActive = x.IsActive
        });
    }

    private async Task<string> GetInstitutionNameAsync(CancellationToken cancellationToken)
    {
        var institutionId = GetInstitutionId();
        if (!institutionId.HasValue) return "QuizSystem";

        return await _db.Institutions
            .Where(x => x.Id == institutionId.Value)
            .Select(x => x.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? "QuizSystem";
    }

    private Guid? GetInstitutionId()
    {
        var rawUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(rawUserId, out var userId)) return null;

        return _db.Users.AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => x.InstitutionId)
            .FirstOrDefault();
    }

    private static string GetReportTitle(string kind) => kind.ToLowerInvariant() switch
    {
        "students" => "تقرير الطلاب",
        "parents" => "تقرير أولياء الأمور",
        "sections" => "تقرير الفصول",
        "subjects" => "تقرير المقررات",
        "teachers" => "تقرير المعلمين",
        _ => "تقرير المدرسة"
    };

    private static string EscapeCsv(string? value)
    {
        value ??= "";
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private static void HeaderCell(TableDescriptor table, string text)
    {
        table.Cell().Background(Colors.Blue.Lighten4).Border(1).Padding(5).Text(text).Bold();
    }

    private static void BodyCell(TableDescriptor table, string? text)
    {
        table.Cell().Border(1).Padding(5).Text(text ?? "");
    }

    private sealed class StudentReportRow
    {
        public string FullName { get; set; } = "";
        public string StudentCode { get; set; } = "";
        public string Grade { get; set; } = "";
        public string Branch { get; set; } = "";
        public string NationalId { get; set; } = "";
        public string Mobile { get; set; } = "";
        public string Nationality { get; set; } = "";
        public bool IsActive { get; set; }
    }
}
