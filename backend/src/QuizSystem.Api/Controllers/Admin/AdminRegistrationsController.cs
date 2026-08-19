using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizSystem.Domain.Entities;
using QuizSystem.Infrastructure.Persistence;
using QuizSystem.Api.Infrastructure.Tenant;

namespace QuizSystem.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/registrations")]
[Authorize(Policy = "AdminOrSupervisor")]
public class AdminRegistrationsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminRegistrationsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var institutionId = await TenantResolver.RequireCurrentInstitutionIdAsync(_db, User, cancellationToken);
        var allowedExamIds = await AllowedExamIds(institutionId, cancellationToken);
        var exams = await _db.Set<Exam>()
            .AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && allowedExamIds.Contains(x.Id))
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new RegistrationSummaryDto
            {
                ExamId = x.Id,
                ExamTitle = x.Title,
                ExamCode = x.ExamCode,
                StartAtUtc = x.StartAtUtc,
                EndAtUtc = x.EndAtUtc,
                RegisteredCount = x.Registrations.Count(r => r.IsActive)
            })
            .ToListAsync(cancellationToken);

        return Ok(exams);
    }

    [HttpGet("exams/{examId:guid}")]
    public async Task<IActionResult> GetExamRegistrations(Guid examId, CancellationToken cancellationToken)
    {
        var institutionId = await TenantResolver.RequireCurrentInstitutionIdAsync(_db, User, cancellationToken);
        await RequireExamAccess(institutionId, examId, cancellationToken);
        var rows = await _db.Set<ExamRegistration>()
            .AsNoTracking()
            .Where(x => x.ExamId == examId && x.Exam.InstitutionId == institutionId && x.IsActive)
            .OrderBy(x => x.StudentProfile.FullName)
            .Select(x => new ExamRegistrationRowDto
            {
                Id = x.Id,
                ExamId = x.ExamId,
                StudentId = x.StudentProfileId,
                StudentName = x.StudentProfile.FullName,
                StudentCode = x.StudentProfile.StudentCode,
                AssignedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }

    [HttpPost("exams/{examId:guid}")]
    public async Task<IActionResult> Register(Guid examId, [FromBody] RegisterCourseStudentRequest request, CancellationToken cancellationToken)
    {
        var institutionId = await TenantResolver.RequireCurrentInstitutionIdAsync(_db, User, cancellationToken);
        await RequireExamAccess(institutionId, examId, cancellationToken);
        if (!await _db.Students.AnyAsync(x => x.Id == request.StudentId && x.InstitutionId == institutionId && x.IsActive, cancellationToken)) throw new KeyNotFoundException("الطالب غير موجود في المؤسسة الحالية");
        var existing = await _db.Registrations.FirstOrDefaultAsync(x => x.InstitutionId == institutionId && x.ExamId == examId && x.StudentProfileId == request.StudentId, cancellationToken);
        if (existing is not null) { existing.IsActive = true; await _db.SaveChangesAsync(cancellationToken); return Ok(new { id = existing.Id }); }
        var row = new ExamRegistration { InstitutionId = institutionId, ExamId = examId, StudentProfileId = request.StudentId, IsActive = true, AssignedByUserId = TenantResolver.GetCurrentUserId(User) ?? throw new UnauthorizedAccessException() };
        _db.Registrations.Add(row); await _db.SaveChangesAsync(cancellationToken); return Ok(new { id = row.Id });
    }

    [HttpGet("exams/{examId:guid}/sections")]
    public async Task<IActionResult> GetExamSections(Guid examId, CancellationToken cancellationToken)
    {
        var institutionId = await TenantResolver.RequireCurrentInstitutionIdAsync(_db, User, cancellationToken);
        await RequireExamAccess(institutionId, examId, cancellationToken);
        var subjectId = await _db.Exams.Where(x => x.Id == examId && x.InstitutionId == institutionId).Select(x => x.SubjectId).SingleAsync(cancellationToken);
        if (!subjectId.HasValue) return Ok(Array.Empty<object>());
        var sections = await _db.ClassSections.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && x.SubjectId == subjectId && x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.AcademicYear, x.Term, activeStudentsCount = x.SectionStudents.Count(s => s.IsActive && s.StudentProfile.IsActive) })
            .ToListAsync(cancellationToken);
        return Ok(sections);
    }

    [HttpPost("exams/{examId:guid}/sections/{sectionId:guid}")]
    public async Task<IActionResult> RegisterSection(Guid examId, Guid sectionId, CancellationToken cancellationToken)
    {
        var institutionId = await TenantResolver.RequireCurrentInstitutionIdAsync(_db, User, cancellationToken);
        await RequireExamAccess(institutionId, examId, cancellationToken);
        var examSubjectId = await _db.Exams.Where(x => x.Id == examId && x.InstitutionId == institutionId).Select(x => x.SubjectId).SingleAsync(cancellationToken);
        var section = await _db.ClassSections.AsNoTracking().FirstOrDefaultAsync(x => x.Id == sectionId && x.InstitutionId == institutionId && x.IsActive, cancellationToken);
        if (section is null) throw new KeyNotFoundException("الشعبة غير موجودة أو غير فعالة.");
        if (!examSubjectId.HasValue || section.SubjectId != examSubjectId.Value) throw new InvalidOperationException("يمكن اختيار شعب المقرر التابع للاختبار فقط.");
        var studentIds = await _db.SectionStudents.Where(x => x.InstitutionId == institutionId && x.ClassSectionId == sectionId && x.IsActive && x.StudentProfile.IsActive).Select(x => x.StudentProfileId).Distinct().ToListAsync(cancellationToken);
        var existing = await _db.Registrations.Where(x => x.InstitutionId == institutionId && x.ExamId == examId && studentIds.Contains(x.StudentProfileId)).ToListAsync(cancellationToken);
        var assignedBy = TenantResolver.GetCurrentUserId(User) ?? throw new UnauthorizedAccessException();
        var added = 0; var reactivated = 0;
        foreach (var studentId in studentIds)
        {
            var row = existing.FirstOrDefault(x => x.StudentProfileId == studentId);
            if (row is null) { _db.Registrations.Add(new ExamRegistration { InstitutionId = institutionId, ExamId = examId, StudentProfileId = studentId, IsActive = true, AssignedByUserId = assignedBy }); added++; }
            else if (!row.IsActive) { row.IsActive = true; row.AssignedByUserId = assignedBy; reactivated++; }
        }
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { sectionId, activeStudents = studentIds.Count, added, reactivated, skipped = studentIds.Count - added - reactivated });
    }

    [HttpGet("exams/{examId:guid}/export")]
    public async Task<IActionResult> ExportExamRegistrations(Guid examId, CancellationToken cancellationToken)
    {
        var institutionId = await TenantResolver.RequireCurrentInstitutionIdAsync(_db, User, cancellationToken);
        await RequireExamAccess(institutionId, examId, cancellationToken);
        var exam = await _db.Set<Exam>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == examId && x.InstitutionId == institutionId, cancellationToken);

        if (exam is null)
            return NotFound(new ProblemDetails
            {
                Title = "Exam not found",
                Detail = "الاختبار غير موجود",
                Status = StatusCodes.Status404NotFound
            });

        var rows = await _db.Set<ExamRegistration>()
            .AsNoTracking()
            .Where(x => x.ExamId == examId && x.IsActive)
            .OrderBy(x => x.StudentProfile.FullName)
            .Select(x => new
            {
                StudentName = x.StudentProfile.FullName,
                StudentCode = x.StudentProfile.StudentCode,
                AssignedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("اسم الطالب,كود الطالب,تاريخ التسجيل");

        foreach (var row in rows)
        {
            sb.Append('"').Append((row.StudentName ?? "").Replace("\"", "\"\"")).Append("\",");
            sb.Append('"').Append((row.StudentCode ?? "").Replace("\"", "\"\"")).Append("\",");
            sb.Append('"').Append(row.AssignedAtUtc.ToString("yyyy-MM-dd HH:mm:ss")).Append('"');
            sb.AppendLine();
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        var fileName = $"registrations_{(exam.ExamCode ?? exam.Id.ToString())}.csv";

        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    [HttpDelete("exams/{examId:guid}")]
    public async Task<IActionResult> ClearExamRegistrations(Guid examId, CancellationToken cancellationToken)
    {
        var institutionId = await TenantResolver.RequireCurrentInstitutionIdAsync(_db, User, cancellationToken);
        await RequireExamAccess(institutionId, examId, cancellationToken);
        var rows = await _db.Set<ExamRegistration>()
            .Where(x => x.ExamId == examId && x.Exam.InstitutionId == institutionId && x.IsActive)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
            return Ok(new { deleted = 0 });

        foreach (var row in rows)
            row.IsActive = false;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { deleted = rows.Count });
    }

    [HttpDelete("{registrationId:guid}")]
    public async Task<IActionResult> DeleteRegistration(Guid registrationId, CancellationToken cancellationToken)
    {
        var institutionId = await TenantResolver.RequireCurrentInstitutionIdAsync(_db, User, cancellationToken);
        var row = await _db.Set<ExamRegistration>()
            .FirstOrDefaultAsync(x => x.Id == registrationId && x.Exam.InstitutionId == institutionId, cancellationToken);

        if (row is null)
            return NotFound(new ProblemDetails
            {
                Title = "Registration not found",
                Detail = "التسجيل غير موجود",
                Status = StatusCodes.Status404NotFound
            });

        await RequireExamAccess(institutionId, row.ExamId, cancellationToken);
        row.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Registration deleted successfully" });
    }

    private async Task<List<Guid>> AllowedExamIds(Guid institutionId, CancellationToken ct)
    {
        var exams = _db.Exams.Where(x => x.InstitutionId == institutionId);
        if (User.IsInRole("CourseSupervisor"))
        {
            var userId = TenantResolver.GetCurrentUserId(User) ?? throw new UnauthorizedAccessException();
            var teacherId = await _db.Users.Where(x => x.Id == userId && x.InstitutionId == institutionId).Select(x => x.TeacherProfileId).FirstOrDefaultAsync(ct);
            exams = exams.Where(x => x.SubjectId.HasValue && teacherId.HasValue && _db.TeacherSubjects.Any(t => t.InstitutionId == institutionId && t.SubjectId == x.SubjectId && t.TeacherProfileId == teacherId && t.IsActive));
        }
        return await exams.Select(x => x.Id).ToListAsync(ct);
    }

    private async Task RequireExamAccess(Guid institutionId, Guid examId, CancellationToken ct)
    {
        if (!(await AllowedExamIds(institutionId, ct)).Contains(examId)) throw new UnauthorizedAccessException("ليس لديك صلاحية لإدارة تسجيلات هذا الاختبار");
    }
}

public class RegistrationSummaryDto
{
    public Guid ExamId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public string ExamCode { get; set; } = string.Empty;
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public int RegisteredCount { get; set; }
}

public class ExamRegistrationRowDto
{
    public Guid Id { get; set; }
    public Guid ExamId { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public DateTime AssignedAtUtc { get; set; }
}
public sealed class RegisterCourseStudentRequest { public Guid StudentId { get; set; } }
