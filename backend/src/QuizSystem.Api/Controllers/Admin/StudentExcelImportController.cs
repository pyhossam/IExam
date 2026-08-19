using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizSystem.Api.Infrastructure.Tenant;
using QuizSystem.Application.Contracts.Auth;
using QuizSystem.Domain.Entities;
using QuizSystem.Domain.Enums;
using QuizSystem.Infrastructure.Persistence;

namespace QuizSystem.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/students/import")]
[Authorize(Policy = "AdminOnly")]
public sealed class StudentExcelImportController(AppDbContext db, IPasswordHasher passwordHasher) : ControllerBase
{
    private static readonly string[] DefaultBranches = ["بنين", "بنات"];

    [HttpGet("template")]
    public async Task<IActionResult> DownloadTemplate(CancellationToken ct)
    {
        var institutionId = await TenantResolver.RequireCurrentInstitutionIdAsync(db, User, ct);
        var grades = await db.GradeLevels.AsNoTracking().Where(x => x.InstitutionId == institutionId && x.IsActive).OrderBy(x => x.Order).Select(x => x.Name).ToListAsync(ct);
        var branches = (await db.Students.AsNoTracking().Where(x => x.InstitutionId == institutionId && x.Branch != null && x.Branch != "").Select(x => x.Branch!).Distinct().ToListAsync(ct)).Concat(DefaultBranches).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Students");
        var headers = new[] { "full_name", "student_code", "user_name", "password", "grade", "branch", "national_id", "mobile", "nationality", "gender" };
        for (var i = 0; i < headers.Length; i++) sheet.Cell(1, i + 1).Value = headers[i];
        var example = new[] { "اسم الطالب", "ST-1001", "student@example.com", "Student@123", "المرحلة المتوسطة", "بنين", "1234567890", "05xxxxxxxx", "سعودي", "Male" };
        for (var i = 0; i < example.Length; i++) sheet.Cell(2, i + 1).Value = example[i];
        sheet.Row(1).Style.Font.Bold = true;
        sheet.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#DCE9FF");
        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents();
        var values = workbook.AddWorksheet("Reference Values");
        values.Cell("A1").Value = "Registered grades"; values.Cell("B1").Value = "Approved branches";
        for (var i = 0; i < grades.Count; i++) values.Cell(i + 2, 1).Value = grades[i];
        for (var i = 0; i < branches.Count; i++) values.Cell(i + 2, 2).Value = branches[i];
        values.Row(1).Style.Font.Bold = true; values.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "QuizSystem_Students_Import_Template.xlsx");
    }

    [HttpPost("preview")]
    [RequestSizeLimit(12_000_000)]
    public async Task<IActionResult> Preview(IFormFile file, CancellationToken ct)
    {
        var institutionId = await TenantResolver.RequireCurrentInstitutionIdAsync(db, User, ct);
        if (file is null || file.Length == 0) return BadRequest(new ProblemDetails { Title = "اختر ملف Excel أولاً." });
        if (!new[] { ".xlsx", ".xlsm" }.Contains(Path.GetExtension(file.FileName).ToLowerInvariant()))
            return BadRequest(new ProblemDetails { Title = "الملف يجب أن يكون بصيغة XLSX." });

        await using var input = file.OpenReadStream();
        using var workbook = new XLWorkbook(input);
        var sheet = workbook.Worksheets.First();
        var header = sheet.Row(1).CellsUsed().ToDictionary(x => Normalize(x.GetString()), x => x.Address.ColumnNumber);
        string Cell(IXLRow row, string name) => header.TryGetValue(Normalize(name), out var col) ? row.Cell(col).GetFormattedString().Trim() : "";
        foreach (var required in new[] { "full_name", "student_code", "user_name", "password", "grade", "branch" })
            if (!header.ContainsKey(required)) return BadRequest(new ProblemDetails { Title = $"العمود المطلوب غير موجود: {required}" });

        var rows = sheet.RowsUsed().Skip(1).Where(x => !x.IsEmpty()).Take(2000).Select((row, index) => new StudentImportRow
        {
            RowNumber = index + 2, FullName = Cell(row, "full_name"), StudentCode = Cell(row, "student_code"),
            UserName = Cell(row, "user_name"), Password = Cell(row, "password"), Grade = Cell(row, "grade"), Branch = Cell(row, "branch"),
            NationalId = Cell(row, "national_id"), Mobile = Cell(row, "mobile"), Nationality = Cell(row, "nationality"), Gender = Cell(row, "gender")
        }).ToList();
        if (rows.Count == 0) return BadRequest(new ProblemDetails { Title = "ملف Excel لا يحتوي على بيانات طلاب." });

        var grades = await db.GradeLevels.AsNoTracking().Where(x => x.InstitutionId == institutionId && x.IsActive)
            .OrderBy(x => x.Order).Select(x => new MappingOption(x.Id.ToString(), x.Name)).ToListAsync(ct);
        var branches = await db.Students.AsNoTracking().Where(x => x.InstitutionId == institutionId && x.Branch != null && x.Branch != "")
            .Select(x => x.Branch!).Distinct().ToListAsync(ct);
        branches = branches.Concat(DefaultBranches).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();

        var gradeValues = rows.Select(x => x.Grade).Where(x => x != "").Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(value => BuildMapping(value, grades)).ToList();
        var branchOptions = branches.Select(x => new MappingOption(x, x)).ToList();
        var branchValues = rows.Select(x => x.Branch).Where(x => x != "").Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(value => BuildMapping(value, branchOptions)).ToList();

        return Ok(new { fileName = file.FileName, rows, gradeOptions = grades, branchOptions, gradeMappings = gradeValues, branchMappings = branchValues });
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm([FromBody] ConfirmStudentImportRequest request, CancellationToken ct)
    {
        var institutionId = await TenantResolver.RequireCurrentInstitutionIdAsync(db, User, ct);
        if (request.Rows.Count is 0 or > 2000) return BadRequest(new ProblemDetails { Title = "عدد صفوف الاستيراد غير صالح." });
        var grades = await db.GradeLevels.Where(x => x.InstitutionId == institutionId && x.IsActive).ToDictionaryAsync(x => x.Id, ct);
        var gradeMap = request.GradeMappings.Where(x => Guid.TryParse(x.TargetValue, out _)).GroupBy(x => Normalize(x.SourceValue)).ToDictionary(x => x.Key, x => Guid.Parse(x.Last().TargetValue));
        var allowedBranches = (await db.Students.Where(x => x.InstitutionId == institutionId && x.Branch != null && x.Branch != "").Select(x => x.Branch!).Distinct().ToListAsync(ct))
            .Concat(DefaultBranches).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var branchMap = request.BranchMappings.Where(x => allowedBranches.Contains(x.TargetValue.Trim())).GroupBy(x => Normalize(x.SourceValue)).ToDictionary(x => x.Key, x => x.Last().TargetValue.Trim());
        var existingCodes = (await db.Students.Where(x => x.InstitutionId == institutionId).Select(x => x.StudentCode).ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existingUsers = (await db.Users.Select(x => x.UserName).ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existingEmails = (await db.Users.Where(x => x.Email != null).Select(x => x.Email!).ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var errors = new List<object>(); var inserted = 0;
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        foreach (var row in request.Rows)
        {
            var rowErrors = new List<string>();
            if (string.IsNullOrWhiteSpace(row.FullName)) rowErrors.Add("اسم الطالب مطلوب");
            if (string.IsNullOrWhiteSpace(row.StudentCode) || existingCodes.Contains(row.StudentCode)) rowErrors.Add("كود الطالب مفقود أو مكرر");
            if (string.IsNullOrWhiteSpace(row.UserName) || existingUsers.Contains(row.UserName)) rowErrors.Add("اسم المستخدم مفقود أو مكرر");
            if (row.UserName.Contains('@') && existingEmails.Contains(row.UserName)) rowErrors.Add("البريد الإلكتروني مستخدم مسبقاً");
            if (string.IsNullOrWhiteSpace(row.Password) || row.Password.Length < 8) rowErrors.Add("كلمة المرور يجب ألا تقل عن 8 أحرف");
            GradeLevel? grade = null;
            var hasMappedGrade = gradeMap.TryGetValue(Normalize(row.Grade), out var gradeId) && grades.TryGetValue(gradeId, out grade);
            if (!hasMappedGrade) rowErrors.Add("لم يتم ربط المرحلة");
            if (!branchMap.TryGetValue(Normalize(row.Branch), out var branch) || string.IsNullOrWhiteSpace(branch)) rowErrors.Add("لم يتم ربط الفرع");
            if (rowErrors.Count > 0) { errors.Add(new { row = row.RowNumber, student = row.FullName, errors = rowErrors }); continue; }

            var student = new StudentProfile { InstitutionId = institutionId, FullName = row.FullName.Trim(), StudentCode = row.StudentCode.Trim(), GradeLevelId = gradeId, Grade = grade!.Name, Branch = branch, NationalId = row.NationalId?.Trim() ?? "", Mobile = row.Mobile?.Trim(), PhoneNumber = row.Mobile?.Trim(), Nationality = row.Nationality?.Trim(), Gender = row.Gender?.Trim() ?? "", IsActive = true };
            db.Students.Add(student);
            db.Users.Add(new AppUser { InstitutionId = institutionId, UserName = row.UserName.Trim(), Email = row.UserName.Contains('@') ? row.UserName.Trim().ToLowerInvariant() : null, PasswordHash = passwordHasher.Hash(row.Password), Role = UserRole.Student, StudentProfile = student, IsActive = true, MustChangePassword = true });
            existingCodes.Add(student.StudentCode); existingUsers.Add(row.UserName); if (row.UserName.Contains('@')) existingEmails.Add(row.UserName); inserted++;
        }
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return Ok(new { inserted, skipped = errors.Count, errors });
    }

    private static ImportValueMapping BuildMapping(string value, IReadOnlyCollection<MappingOption> options)
    {
        var normalizedValue = Normalize(value);
        var match = options.FirstOrDefault(x => Normalize(x.Label) == normalizedValue);
        if (match is null && normalizedValue.Length >= 3)
        {
            var closeMatches = options.Where(x => Normalize(x.Label).Contains(normalizedValue) || normalizedValue.Contains(Normalize(x.Label))).ToList();
            if (closeMatches.Count == 1) match = closeMatches[0];
        }
        return new ImportValueMapping(value, match?.Value, match is not null);
    }
    private static string Normalize(string value) => string.Concat((value ?? "").Trim().ToLowerInvariant().Where(c => !char.IsWhiteSpace(c) && c != '_' && c != '-'));
}

public sealed record MappingOption(string Value, string Label);
public sealed record ImportValueMapping(string SourceValue, string? TargetValue, bool AutoMatched);
public sealed record ImportMappingSelection(string SourceValue, string TargetValue);
public sealed class StudentImportRow { public int RowNumber { get; set; } public string FullName { get; set; } = ""; public string StudentCode { get; set; } = ""; public string UserName { get; set; } = ""; public string Password { get; set; } = ""; public string Grade { get; set; } = ""; public string Branch { get; set; } = ""; public string? NationalId { get; set; } public string? Mobile { get; set; } public string? Nationality { get; set; } public string? Gender { get; set; } }
public sealed class ConfirmStudentImportRequest { public List<StudentImportRow> Rows { get; set; } = []; public List<ImportMappingSelection> GradeMappings { get; set; } = []; public List<ImportMappingSelection> BranchMappings { get; set; } = []; }
