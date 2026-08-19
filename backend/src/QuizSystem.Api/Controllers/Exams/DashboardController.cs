using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizSystem.Api.Infrastructure.Tenant;
using QuizSystem.Application.Contracts.Attempts;
using QuizSystem.Application.Contracts.Auth;
using QuizSystem.Application.Contracts.Exams;
using QuizSystem.Application.Contracts.Imports;
using QuizSystem.Application.Contracts.Portals;
using QuizSystem.Application.Contracts.Reports;
using QuizSystem.Application.DTOs;
using QuizSystem.Infrastructure.Persistence;
using QuizSystem.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace QuizSystem.Api.Controllers.Exams;
[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = "AdminOrSupervisor")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardAnalyticsService _dashboardAnalyticsService;
    private readonly AppDbContext _db;

    public DashboardController(IDashboardAnalyticsService dashboardAnalyticsService, AppDbContext db)
    {
        _dashboardAnalyticsService = dashboardAnalyticsService;
        _db = db;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> Overview(CancellationToken cancellationToken)
    {
        var institutionId = await TenantResolver.RequireCurrentInstitutionIdAsync(_db, User, cancellationToken);
        if (User.IsInRole("CourseSupervisor"))
        {
            var userId = TenantResolver.GetCurrentUserId(User) ?? throw new UnauthorizedAccessException();
            var teacherId = await _db.Users.Where(x => x.Id == userId && x.InstitutionId == institutionId).Select(x => x.TeacherProfileId).FirstOrDefaultAsync(cancellationToken);
            if (!teacherId.HasValue) throw new InvalidOperationException("حساب مشرف المقرر غير مرتبط بملف معلم");
            var subjectIds = await _db.TeacherSubjects.Where(x => x.InstitutionId == institutionId && x.TeacherProfileId == teacherId && x.IsActive).Select(x => x.SubjectId).Distinct().ToListAsync(cancellationToken);
            var courses = await _db.Subjects.Where(x => x.InstitutionId == institutionId && subjectIds.Contains(x.Id)).OrderBy(x => x.Name).Select(x => new AssignedCourseDto { Id = x.Id, Name = x.Name, Code = x.Code, ClosCount = _db.CourseLearningOutcomes.Count(c => c.SubjectId == x.Id && c.IsActive), ExamsCount = _db.Exams.Count(e => e.SubjectId == x.Id) }).ToListAsync(cancellationToken);
            var examIds = await _db.Exams.Where(x => x.InstitutionId == institutionId && x.SubjectId.HasValue && subjectIds.Contains(x.SubjectId.Value)).Select(x => x.Id).ToListAsync(cancellationToken);
            return Ok(new DashboardOverviewDto
            {
                Role = "CourseSupervisor", InstitutionName = await _db.Institutions.Where(x => x.Id == institutionId).Select(x => x.Name).FirstOrDefaultAsync(cancellationToken), AssignedCourses = courses,
                ExamsCount = examIds.Count,
                RegistrationsCount = await _db.Registrations.CountAsync(x => examIds.Contains(x.ExamId), cancellationToken),
                AttemptsCount = await _db.Attempts.CountAsync(x => examIds.Contains(x.ExamId), cancellationToken),
                StudentsCount = await _db.Registrations.Where(x => examIds.Contains(x.ExamId) && x.IsActive).Select(x => x.StudentProfileId).Distinct().CountAsync(cancellationToken)
            });
        }
        return Ok(await _dashboardAnalyticsService.GetOverviewAsync(institutionId, cancellationToken));
    }

    [HttpGet("exams/{examId:guid}")]
    public async Task<IActionResult> ExamAnalytics(Guid examId, CancellationToken cancellationToken)
    {
        var institutionId = await TenantResolver.RequireCurrentInstitutionIdAsync(_db, User, cancellationToken);
        if (User.IsInRole("CourseSupervisor"))
        {
            var userId = TenantResolver.GetCurrentUserId(User) ?? throw new UnauthorizedAccessException();
            var teacherId = await _db.Users.Where(x => x.Id == userId && x.InstitutionId == institutionId).Select(x => x.TeacherProfileId).FirstOrDefaultAsync(cancellationToken);
            var allowed = await _db.Exams.AnyAsync(x => x.Id == examId && x.InstitutionId == institutionId && x.SubjectId.HasValue && teacherId.HasValue && _db.TeacherSubjects.Any(t => t.InstitutionId == institutionId && t.SubjectId == x.SubjectId && t.TeacherProfileId == teacherId && t.IsActive), cancellationToken);
            if (!allowed) throw new UnauthorizedAccessException("ليس لديك صلاحية لعرض تقرير هذا الاختبار");
        }
        return Ok(await _dashboardAnalyticsService.GetExamAnalyticsAsync(institutionId, examId, cancellationToken));
    }
}
