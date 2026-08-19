using Microsoft.EntityFrameworkCore;
using QuizSystem.Application.Contracts.Attempts;
using QuizSystem.Application.Contracts.Auth;
using QuizSystem.Application.Contracts.Exams;
using QuizSystem.Application.Contracts.Imports;
using QuizSystem.Application.Contracts.Portals;
using QuizSystem.Application.Contracts.Reports;
using QuizSystem.Application.DTOs;
using QuizSystem.Domain.Entities;
using QuizSystem.Infrastructure.Persistence;

namespace QuizSystem.Infrastructure.Services.Portals;
public class PortalService : IPortalService
{
    private readonly AppDbContext _db;

    public PortalService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<StudentPortalDashboardDto> GetStudentDashboardAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var student = await _db.Students.FirstOrDefaultAsync(x => x.Id == studentId, cancellationToken)
            ?? throw new InvalidOperationException("Student not found");

        var now = DateTime.UtcNow;

        var registrations = await _db.Registrations
            .Where(x => x.StudentProfileId == studentId && x.IsActive)
            .Include(x => x.Exam)
            .ToListAsync(cancellationToken);

        var attempts = await _db.Attempts
            .Where(x => x.StudentProfileId == studentId)
            .Include(x => x.Exam)
            .OrderByDescending(x => x.StartedAtUtc)
            .ToListAsync(cancellationToken);

        return new StudentPortalDashboardDto
        {
            StudentId = student.Id,
            StudentName = student.FullName,
            StudentCode = student.StudentCode,
            Grade = student.Grade,
            AvailableExams = registrations.Select(r =>
            {
                var hasSubmitted = attempts.Any(a => a.ExamId == r.ExamId);
                var status = now < r.Exam.StartAtUtc
                    ? "NotStarted"
                    : now > r.Exam.EndAtUtc
                        ? "Ended"
                        : "Available";

                return new StudentPortalExamItemDto
                {
                    ExamId = r.ExamId,
                    Title = r.Exam.Title,
                    ExamCode = r.Exam.ExamCode,
                    StartAtUtc = r.Exam.StartAtUtc,
                    EndAtUtc = r.Exam.EndAtUtc,
                    AvailabilityStatus = status,
                    CanStart = status == "Available" && !hasSubmitted,
                    HasSubmitted = hasSubmitted
                };
            }).ToList(),
            AttemptHistory = attempts.Select(a => new AttemptListItemDto
            {
                AttemptId = a.Id,
                StudentId = a.StudentProfileId,
                StudentName = student.FullName,
                StudentCode = student.StudentCode,
                ExamId = a.ExamId,
                ExamTitle = a.Exam.Title,
                Score = a.Score ?? 0,
                TotalQuestions = a.TotalQuestions ?? 0,
                Percentage = a.Percentage ?? 0,
                Status = a.Status.ToString(),
                StartedAtUtc = a.StartedAtUtc,
                SubmittedAtUtc = a.SubmittedAtUtc
            }).ToList()
        };
    }

    public async Task<ParentPortalDashboardDto> GetParentDashboardAsync(Guid parentId, CancellationToken cancellationToken = default)
    {
        var parent = await _db.Parents.FirstOrDefaultAsync(x => x.Id == parentId, cancellationToken)
            ?? throw new InvalidOperationException("Parent not found");

        var links = await _db.ParentStudentLinks
            .Where(x => x.ParentProfileId == parentId)
            .Include(x => x.StudentProfile)
            .ToListAsync(cancellationToken);

        var studentIds = links.Select(x => x.StudentProfileId).ToList();

        var attempts = await _db.Attempts
            .Where(x => studentIds.Contains(x.StudentProfileId))
            .Include(x => x.StudentProfile)
            .Include(x => x.Exam)
            .OrderByDescending(x => x.SubmittedAtUtc)
            .ToListAsync(cancellationToken);

        return new ParentPortalDashboardDto
        {
            ParentId = parent.Id,
            ParentName = parent.FullName,
            ParentCode = parent.ParentCode,
            Children = links.Select(x => new ParentChildSummaryDto
            {
                StudentId = x.StudentProfile.Id,
                StudentName = x.StudentProfile.FullName,
                StudentCode = x.StudentProfile.StudentCode,
                Grade = x.StudentProfile.Grade
            }).ToList(),
            ChildrenResults = attempts.Select(a => new ParentChildResultResponse
            {
                StudentName = a.StudentProfile.FullName,
                StudentCode = a.StudentProfile.StudentCode,
                ExamTitle = a.Exam.Title,
                ExamCode = a.Exam.ExamCode,
                Score = a.Score ?? 0,
                TotalQuestions = a.TotalQuestions ?? 0,
                Percentage = a.Percentage ?? 0,
                SubmittedAtUtc = a.SubmittedAtUtc
            }).ToList()
        };
    }

    public async Task<List<LeaderboardItemDto>> GetExamLeaderboardAsync(Guid examId, CancellationToken cancellationToken = default)
    {
        var attempts = await _db.Attempts
            .Where(x => x.ExamId == examId)
            .Include(x => x.StudentProfile)
            .OrderByDescending(x => x.Percentage)
            .ThenByDescending(x => x.Score)
            .ThenBy(x => x.SubmittedAtUtc)
            .ToListAsync(cancellationToken);

        var rank = 1;
        return attempts.Select(a => new LeaderboardItemDto
        {
            Rank = rank++,
            StudentId = a.StudentProfileId,
            StudentName = a.StudentProfile.FullName,
            StudentCode = a.StudentProfile.StudentCode,
            Score = a.Score ?? 0,
            TotalQuestions = a.TotalQuestions ?? 0,
            Percentage = a.Percentage ?? 0,
            SubmittedAtUtc = a.SubmittedAtUtc
        }).ToList();
    }

    public int? Score { get; set; }
    public int? TotalQuestions { get; set; }
    public decimal? Percentage { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }

}
