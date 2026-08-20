using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using UglyToad.PdfPig;
using QuizSystem.Api.Infrastructure.Tenant;
using QuizSystem.Application.Contracts.Exams;
using QuizSystem.Domain.Entities;
using QuizSystem.Domain.Enums;
using QuizSystem.Infrastructure.Persistence;

namespace QuizSystem.Api.Controllers.Courses;

[ApiController]
[Route("api/courses")]
[Authorize(Policy = "AdminOrSupervisor")]
public sealed class CourseOutcomesController(AppDbContext db, IAiQuestionGenerator aiGenerator) : ControllerBase
{
    [HttpGet("assigned")]
    public async Task<IActionResult> Assigned(CancellationToken ct)
    {
        var tenant = await TenantResolver.RequireCurrentInstitutionIdAsync(db, User, ct);
        var query = db.Subjects.AsNoTracking().Where(x => x.InstitutionId == tenant && x.IsActive);
        if (User.IsInRole("CourseSupervisor") || User.IsInRole("Teacher"))
        {
            var userId = TenantResolver.GetCurrentUserId(User) ?? throw new UnauthorizedAccessException();
            var teacherId = await db.Users.Where(x => x.Id == userId && x.InstitutionId == tenant).Select(x => x.TeacherProfileId).FirstOrDefaultAsync(ct);
            if (User.IsInRole("Teacher")) query = query.Where(x => teacherId.HasValue && db.ClassSections.Any(s => s.InstitutionId == tenant && s.SubjectId == x.Id && s.TeacherProfileId == teacherId && s.IsActive));
            else query = query.Where(x => teacherId.HasValue && db.TeacherSubjects.Any(t => t.InstitutionId == tenant && t.SubjectId == x.Id && t.TeacherProfileId == teacherId && t.IsActive));
        }
        return Ok(await query.OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.Code }).ToListAsync(ct));
    }

    [HttpGet("{subjectId:guid}/clos")]
    public async Task<IActionResult> List(Guid subjectId, CancellationToken ct)
    {
        var tenant = await RequireAccess(subjectId, false, ct);
        return Ok(await db.CourseLearningOutcomes.AsNoTracking()
            .Where(x => x.InstitutionId == tenant && x.SubjectId == subjectId)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Code)
            .Select(x => new { x.Id, x.SubjectId, x.Code, x.Description, domain = x.Domain.ToString(), cognitiveLevel = x.CognitiveLevel.ToString(), x.TargetPercentage, x.DisplayOrder, x.IsActive })
            .ToListAsync(ct));
    }

    [HttpPost("{subjectId:guid}/clos")]
    public async Task<IActionResult> Create(Guid subjectId, [FromBody] UpsertCloRequest request, CancellationToken ct)
    {
        var tenant = await RequireAccess(subjectId, true, ct);
        Validate(request);
        var code = request.Code.Trim().ToUpperInvariant();
        if (await db.CourseLearningOutcomes.AnyAsync(x => x.InstitutionId == tenant && x.SubjectId == subjectId && x.Code == code, ct))
            throw new InvalidOperationException("رمز مخرج التعلم مستخدم بالفعل في هذا المقرر");
        var entity = new CourseLearningOutcome { InstitutionId = tenant, SubjectId = subjectId, Code = code, Description = request.Description.Trim(), Domain = ParseDomain(request.Domain), CognitiveLevel = ParseLevel(request.CognitiveLevel), TargetPercentage = request.TargetPercentage, DisplayOrder = request.DisplayOrder, IsActive = request.IsActive };
        db.CourseLearningOutcomes.Add(entity);
        await db.SaveChangesAsync(ct);
        return Ok(new { entity.Id });
    }

    [HttpPut("{subjectId:guid}/clos/{id:guid}")]
    public async Task<IActionResult> Update(Guid subjectId, Guid id, [FromBody] UpsertCloRequest request, CancellationToken ct)
    {
        var tenant = await RequireAccess(subjectId, true, ct);
        Validate(request);
        var entity = await db.CourseLearningOutcomes.FirstOrDefaultAsync(x => x.Id == id && x.SubjectId == subjectId && x.InstitutionId == tenant, ct) ?? throw new KeyNotFoundException("مخرج التعلم غير موجود");
        var code = request.Code.Trim().ToUpperInvariant();
        if (await db.CourseLearningOutcomes.AnyAsync(x => x.Id != id && x.InstitutionId == tenant && x.SubjectId == subjectId && x.Code == code, ct)) throw new InvalidOperationException("رمز مخرج التعلم مستخدم بالفعل");
        entity.Code = code; entity.Description = request.Description.Trim(); entity.Domain = ParseDomain(request.Domain); entity.CognitiveLevel = ParseLevel(request.CognitiveLevel); entity.TargetPercentage = request.TargetPercentage; entity.DisplayOrder = request.DisplayOrder; entity.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{subjectId:guid}/clos/{id:guid}")]
    public async Task<IActionResult> Delete(Guid subjectId, Guid id, CancellationToken ct)
    {
        var tenant = await RequireAccess(subjectId, true, ct);
        var entity = await db.CourseLearningOutcomes.FirstOrDefaultAsync(x => x.Id == id && x.SubjectId == subjectId && x.InstitutionId == tenant, ct) ?? throw new KeyNotFoundException("مخرج التعلم غير موجود");
        if (await db.Questions.AnyAsync(x => x.CourseLearningOutcomeId == id, ct)) throw new InvalidOperationException("لا يمكن حذف مخرج مرتبط بأسئلة؛ يمكن تعطيله بدلاً من ذلك");
        db.Remove(entity); await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpGet("clos/template")]
    [AllowAnonymous]
    public IActionResult DownloadTemplate()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("CLO");
        string[] headers = ["Code", "Description", "Domain", "CognitiveLevel", "TargetPercentage", "DisplayOrder"];
        for (var i = 0; i < headers.Length; i++) sheet.Cell(1, i + 1).Value = headers[i];
        sheet.Cell(2, 1).Value = "CLO1"; sheet.Cell(2, 2).Value = "يحلل الطالب المفاهيم الأساسية للمقرر"; sheet.Cell(2, 3).Value = "Knowledge"; sheet.Cell(2, 4).Value = "Analyze"; sheet.Cell(2, 5).Value = 70; sheet.Cell(2, 6).Value = 1;
        sheet.Row(1).Style.Font.Bold = true; sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream(); workbook.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "CLO-Template.xlsx");
    }

    [HttpPost("{subjectId:guid}/clos/import")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> Import(Guid subjectId, IFormFile file, CancellationToken ct)
    {
        var tenant = await RequireAccess(subjectId, true, ct);
        if (file.Length == 0) throw new InvalidOperationException("الملف فارغ");
        if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("استيراد CLO يتطلب نموذج Excel بصيغة xlsx؛ يمكن إرفاق PDF كمرجع فقط وليس كمصدر بيانات موثوق");
        using var stream = file.OpenReadStream(); using var workbook = new XLWorkbook(stream); var sheet = workbook.Worksheet(1); var added = 0; var updated = 0;
        foreach (var row in sheet.RowsUsed().Skip(1))
        {
            var code = row.Cell(1).GetString().Trim().ToUpperInvariant(); var description = row.Cell(2).GetString().Trim();
            if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(description)) continue;
            var request = new UpsertCloRequest(code, description, row.Cell(3).GetString(), row.Cell(4).GetString(), row.Cell(5).GetValue<decimal>(), row.Cell(6).GetValue<int>(), true); Validate(request);
            var entity = await db.CourseLearningOutcomes.FirstOrDefaultAsync(x => x.InstitutionId == tenant && x.SubjectId == subjectId && x.Code == code, ct);
            if (entity is null) { entity = new CourseLearningOutcome { InstitutionId = tenant, SubjectId = subjectId, Code = code }; db.Add(entity); added++; } else updated++;
            entity.Description = description; entity.Domain = ParseDomain(request.Domain); entity.CognitiveLevel = ParseLevel(request.CognitiveLevel); entity.TargetPercentage = request.TargetPercentage; entity.DisplayOrder = request.DisplayOrder; entity.IsActive = true;
        }
        await db.SaveChangesAsync(ct); return Ok(new { added, updated });
    }

    [HttpPost("{subjectId:guid}/clos/ai-preview")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> GenerateAiPreview(Guid subjectId, [FromForm] int count, [FromForm] IFormFile file, CancellationToken ct)
    {
        await RequireAccess(subjectId, true, ct);
        if (count is < 1 or > 15) throw new InvalidOperationException("عدد مخرجات التعلم يجب أن يكون بين 1 و15.");
        if (file is null || file.Length == 0) throw new InvalidOperationException("اختر ملف PDF الخاص بالمقرر.");
        if (!Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("يجب أن يكون محتوى المقرر ملف PDF.");
        var subject = await db.Subjects.AsNoTracking().Where(x => x.Id == subjectId).Select(x => new { x.Name, x.Code }).SingleAsync(ct);
        var extracted = await ExtractPdfText(file, ct);
        var summary = await aiGenerator.SummarizeEducationalContentAsync(extracted, $"المقرر: {subject.Name} ({subject.Code})", ct);
        var clos = await aiGenerator.GenerateClosAsync(subject.Name, summary, count, ct);
        return Ok(new { fileName = file.FileName, summary, clos });
    }

    [HttpPost("{subjectId:guid}/clos/ai-approve")]
    public async Task<IActionResult> ApproveAiPreview(Guid subjectId, [FromBody] ApproveGeneratedClosRequest request, CancellationToken ct)
    {
        var tenant = await RequireAccess(subjectId, true, ct);
        if (request.Clos.Count == 0) throw new InvalidOperationException("اختر مخرج تعلم واحدًا على الأقل للاعتماد.");
        if (request.Clos.Count > 15) throw new InvalidOperationException("لا يمكن اعتماد أكثر من 15 مخرجًا دفعة واحدة.");
        var added = 0; var updated = 0;
        foreach (var item in request.Clos)
        {
            Validate(item);
            var code = item.Code.Trim().ToUpperInvariant();
            var entity = await db.CourseLearningOutcomes.FirstOrDefaultAsync(x => x.InstitutionId == tenant && x.SubjectId == subjectId && x.Code == code, ct);
            if (entity is null) { entity = new CourseLearningOutcome { InstitutionId = tenant, SubjectId = subjectId, Code = code }; db.Add(entity); added++; } else updated++;
            entity.Description = item.Description.Trim(); entity.Domain = ParseDomain(item.Domain); entity.CognitiveLevel = ParseLevel(item.CognitiveLevel);
            entity.TargetPercentage = item.TargetPercentage; entity.DisplayOrder = item.DisplayOrder; entity.IsActive = true;
        }
        await db.SaveChangesAsync(ct);
        return Ok(new { added, updated, rejected = request.RejectedCount });
    }

    private static async Task<string> ExtractPdfText(IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        using var document = PdfDocument.Open(stream);
        var text = new StringBuilder();
        foreach (var page in document.GetPages()) { ct.ThrowIfCancellationRequested(); text.AppendLine(page.Text); if (text.Length >= 40_000) break; }
        var result = text.ToString().Trim();
        if (string.IsNullOrWhiteSpace(result)) throw new InvalidOperationException("تعذر قراءة نص ملف PDF. استخدم ملفًا يحتوي على نص قابل للبحث وليس صورًا ممسوحة فقط.");
        return result[..Math.Min(result.Length, 40_000)];
    }

    [HttpPut("{subjectId:guid}/supervisors")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> AssignSupervisors(Guid subjectId, [FromBody] AssignSupervisorsRequest request, CancellationToken ct)
    {
        var tenant = await RequireAccess(subjectId, false, ct); var ids = request.TeacherProfileIds.Distinct().ToList();
        var valid = await db.Users.Where(x => x.InstitutionId == tenant && x.Role == UserRole.CourseSupervisor && x.TeacherProfileId.HasValue && ids.Contains(x.TeacherProfileId.Value)).Select(x => x.TeacherProfileId!.Value).ToListAsync(ct);
        if (valid.Count != ids.Count) throw new InvalidOperationException("يجب أن يكون كل مشرف مستخدماً فعالاً بصلاحية مشرف مقرر داخل المؤسسة");
        var supervisorTeacherIds = await db.Users.Where(x => x.InstitutionId == tenant && x.Role == UserRole.CourseSupervisor && x.TeacherProfileId.HasValue).Select(x => x.TeacherProfileId!.Value).ToListAsync(ct);
        var old = await db.TeacherSubjects.Where(x => x.InstitutionId == tenant && x.SubjectId == subjectId && supervisorTeacherIds.Contains(x.TeacherProfileId)).ToListAsync(ct); db.TeacherSubjects.RemoveRange(old);
        db.TeacherSubjects.AddRange(valid.Select(id => new TeacherSubject { InstitutionId = tenant, SubjectId = subjectId, TeacherProfileId = id, IsActive = true })); await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpGet("supervisor-assignments")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> SupervisorAssignments(CancellationToken ct)
    {
        var tenant = await TenantResolver.RequireCurrentInstitutionIdAsync(db, User, ct);
        var supervisors = await db.Users.AsNoTracking()
            .Where(x => x.InstitutionId == tenant && x.Role == UserRole.CourseSupervisor && x.IsActive && x.TeacherProfileId.HasValue)
            .Select(x => new { UserId = x.Id, x.UserName, TeacherProfileId = x.TeacherProfileId!.Value, Name = x.TeacherProfile != null ? x.TeacherProfile.FullName : x.UserName })
            .OrderBy(x => x.Name).ToListAsync(ct);
        var supervisorIds = supervisors.Select(x => x.TeacherProfileId).ToList();
        var links = await db.TeacherSubjects.AsNoTracking().Where(x => x.InstitutionId == tenant && x.IsActive && supervisorIds.Contains(x.TeacherProfileId))
            .Select(x => new { x.SubjectId, x.TeacherProfileId }).ToListAsync(ct);
        return Ok(new { supervisors, assignments = links.GroupBy(x => x.SubjectId).Select(g => new { SubjectId = g.Key, TeacherProfileIds = g.Select(x => x.TeacherProfileId).Distinct().ToList() }) });
    }

    [HttpGet("{subjectId:guid}/clo-report")]
    public async Task<IActionResult> Report(Guid subjectId, CancellationToken ct)
    {
        var tenant = await RequireAccess(subjectId, false, ct);
        var currentUserId = TenantResolver.GetCurrentUserId(User);
        var teacherOnly = User.IsInRole("Teacher");
        var rows = await db.CourseLearningOutcomes.Where(c => c.InstitutionId == tenant && c.SubjectId == subjectId).Select(c => new
        {
            c.Id, c.Code, c.Description, c.TargetPercentage,
            Questions = db.Questions.Count(q => q.CourseLearningOutcomeId == c.Id && (!teacherOnly || q.Exam.CreatedByUserId == currentUserId)),
            Answered = db.AttemptAnswers.Count(a => a.ExamQuestion.CourseLearningOutcomeId == c.Id && (!teacherOnly || a.ExamQuestion.Exam.CreatedByUserId == currentUserId)),
            Correct = db.AttemptAnswers.Count(a => a.ExamQuestion.CourseLearningOutcomeId == c.Id && a.IsCorrect && (!teacherOnly || a.ExamQuestion.Exam.CreatedByUserId == currentUserId))
        }).ToListAsync(ct);
        return Ok(rows.Select(x => new { x.Id, x.Code, x.Description, x.TargetPercentage, x.Questions, x.Answered, x.Correct, AttainmentPercentage = x.Answered == 0 ? 0 : Math.Round(x.Correct * 100m / x.Answered, 2), Achieved = x.Answered > 0 && x.Correct * 100m / x.Answered >= x.TargetPercentage }));
    }

    [HttpGet("{subjectId:guid}/bloom-report")]
    public async Task<IActionResult> BloomReport(Guid subjectId, CancellationToken ct)
    {
        var tenant = await RequireAccess(subjectId, false, ct);
        var currentUserId = TenantResolver.GetCurrentUserId(User); var teacherOnly = User.IsInRole("Teacher");
        var questions = await db.Questions.AsNoTracking()
            .Where(q => q.Exam.InstitutionId == tenant && q.Exam.SubjectId == subjectId && (!teacherOnly || q.Exam.CreatedByUserId == currentUserId))
            .Select(q => new { q.Id, q.CognitiveLevel })
            .ToListAsync(ct);
        var ids = questions.Select(x => x.Id).ToList();
        var answers = await db.AttemptAnswers.AsNoTracking().Where(a => ids.Contains(a.ExamQuestionId)).Select(a => new { a.ExamQuestionId, a.IsCorrect }).ToListAsync(ct);
        var rows = questions.GroupBy(q => q.CognitiveLevel).Select(g =>
        {
            var questionIds = g.Select(q => q.Id).ToHashSet(); var measured = answers.Where(a => questionIds.Contains(a.ExamQuestionId)).ToList();
            return new { CognitiveLevel = g.Key.ToString(), Questions = g.Count(), Answered = measured.Count, Correct = measured.Count(a => a.IsCorrect), AttainmentPercentage = measured.Count == 0 ? 0 : Math.Round(measured.Count(a => a.IsCorrect) * 100m / measured.Count, 2) };
        });
        return Ok(rows);
    }

    private async Task<Guid> RequireAccess(Guid subjectId, bool write, CancellationToken ct)
    {
        var tenant = await TenantResolver.RequireCurrentInstitutionIdAsync(db, User, ct);
        if (!await db.Subjects.AnyAsync(x => x.Id == subjectId && x.InstitutionId == tenant, ct)) throw new KeyNotFoundException("المقرر غير موجود في المؤسسة الحالية");
        if (User.IsInRole("CourseSupervisor") || User.IsInRole("Teacher"))
        {
            var userId = TenantResolver.GetCurrentUserId(User) ?? throw new UnauthorizedAccessException();
            var teacherId = await db.Users.Where(x => x.Id == userId && x.InstitutionId == tenant).Select(x => x.TeacherProfileId).FirstOrDefaultAsync(ct);
            var allowed = teacherId.HasValue && (User.IsInRole("Teacher")
                ? await db.ClassSections.AnyAsync(x => x.InstitutionId == tenant && x.SubjectId == subjectId && x.TeacherProfileId == teacherId && x.IsActive, ct)
                : await db.TeacherSubjects.AnyAsync(x => x.InstitutionId == tenant && x.SubjectId == subjectId && x.TeacherProfileId == teacherId && x.IsActive, ct));
            if (!allowed) throw new UnauthorizedAccessException("ليس لديك تكليف نشط على هذا المقرر");
            if (write && User.IsInRole("Teacher")) throw new UnauthorizedAccessException("إدارة مخرجات CLO متاحة لمشرف المقرر فقط.");
        }
        return tenant;
    }

    private static void Validate(UpsertCloRequest r) { if (string.IsNullOrWhiteSpace(r.Code)) throw new InvalidOperationException("رمز CLO مطلوب"); if (string.IsNullOrWhiteSpace(r.Description) || r.Description.Trim().Length < 10) throw new InvalidOperationException("اكتب مخرج تعلم واضحاً وقابلاً للقياس"); if (r.TargetPercentage is < 0 or > 100) throw new InvalidOperationException("نسبة الاستهداف يجب أن تكون بين 0 و100"); _ = ParseDomain(r.Domain); _ = ParseLevel(r.CognitiveLevel); }
    private static CloDomain ParseDomain(string value) => Enum.TryParse<CloDomain>(value, true, out var result) ? result : throw new InvalidOperationException("المجال يجب أن يكون Knowledge أو Skills أو Values");
    private static CognitiveLevel ParseLevel(string value) => Enum.TryParse<CognitiveLevel>(value, true, out var result) ? result : throw new InvalidOperationException("مستوى Bloom غير صحيح");
}

public sealed record UpsertCloRequest(string Code, string Description, string Domain, string CognitiveLevel, decimal TargetPercentage, int DisplayOrder, bool IsActive = true);
public sealed record AssignSupervisorsRequest(List<Guid> TeacherProfileIds);
public sealed record ApproveGeneratedClosRequest(List<UpsertCloRequest> Clos, int RejectedCount = 0);
