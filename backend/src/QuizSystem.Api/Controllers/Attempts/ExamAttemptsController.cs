using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizSystem.Application.Contracts.Attempts;
using QuizSystem.Application.Contracts.Auth;
using QuizSystem.Application.Contracts.Exams;
using QuizSystem.Application.Contracts.Imports;
using QuizSystem.Application.Contracts.Portals;
using QuizSystem.Application.Contracts.Reports;
using QuizSystem.Application.DTOs;
using QuizSystem.Infrastructure.Persistence;
using QuizSystem.Infrastructure.Services;
using QuizSystem.Api.Infrastructure.Tenant;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace QuizSystem.Api.Controllers.Attempts;
[ApiController]
[Route("api/exams")]
[Authorize(Policy = "AdminOrSupervisor")]
public class ExamAttemptsController : ControllerBase
{
    private readonly IAttemptManagementService _attemptManagementService;
    private readonly AppDbContext _db;

    public ExamAttemptsController(IAttemptManagementService attemptManagementService, AppDbContext db)
    {
        _attemptManagementService = attemptManagementService;
        _db = db;
    }

    [HttpGet("{examId:guid}/attempts")]
    public async Task<IActionResult> GetExamAttempts(Guid examId, CancellationToken cancellationToken)
    { await RequireExamAccess(examId, cancellationToken); return Ok(await _attemptManagementService.GetExamAttemptsAsync(examId, cancellationToken)); }

    [HttpGet("attempts/{attemptId:guid}")]
    public async Task<IActionResult> GetAttemptDetails(Guid attemptId, CancellationToken cancellationToken)
    {
        var examId = await _db.Attempts.Where(x => x.Id == attemptId).Select(x => x.ExamId).FirstOrDefaultAsync(cancellationToken);
        await RequireExamAccess(examId, cancellationToken);
        var details = await _attemptManagementService.GetAttemptDetailsAsync(attemptId, cancellationToken);
        if (details.Status == "Started") throw new InvalidOperationException("لا يمكن مراجعة ورقة الاختبار قبل تسليمها");
        return Ok(details);
    }

    [HttpDelete("attempts/{attemptId:guid}")]
    public async Task<IActionResult> ResetAttempt(Guid attemptId, CancellationToken cancellationToken)
    {
        var examId = await _db.Attempts.Where(x => x.Id == attemptId).Select(x => x.ExamId).FirstOrDefaultAsync(cancellationToken);
        await RequireExamAccess(examId, cancellationToken);
        await _attemptManagementService.ResetAttemptAsync(attemptId, cancellationToken);
        return Ok(new { message = "Attempt reset successfully" });
    }

    private async Task RequireExamAccess(Guid examId, CancellationToken ct)
    {
        var tenant = await TenantResolver.RequireCurrentInstitutionIdAsync(_db, User, ct);
        var exam = await _db.Exams.AsNoTracking().FirstOrDefaultAsync(x => x.Id == examId && x.InstitutionId == tenant, ct)
            ?? throw new KeyNotFoundException("الاختبار غير موجود");
        if (!User.IsInRole("CourseSupervisor")) return;
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var teacherId = await _db.Users.Where(x => x.Id == userId && x.InstitutionId == tenant).Select(x => x.TeacherProfileId).FirstOrDefaultAsync(ct);
        if (!teacherId.HasValue || !exam.SubjectId.HasValue || !await _db.TeacherSubjects.AnyAsync(x => x.InstitutionId == tenant && x.TeacherProfileId == teacherId && x.SubjectId == exam.SubjectId && x.IsActive, ct))
            throw new UnauthorizedAccessException("ليس لديك صلاحية لمراجعة محاولات هذا المقرر");
    }
}
