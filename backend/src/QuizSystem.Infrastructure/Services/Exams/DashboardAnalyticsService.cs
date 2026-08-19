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

namespace QuizSystem.Infrastructure.Services.Exams;
public class DashboardAnalyticsService : IDashboardAnalyticsService
{
    private readonly AppDbContext _db;

    public DashboardAnalyticsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardOverviewDto> GetOverviewAsync(Guid institutionId, CancellationToken cancellationToken = default)
    {
        return new DashboardOverviewDto
        {
            Role = "Administration",
            InstitutionName = await _db.Institutions
                .Where(x => x.Id == institutionId)
                .Select(x => x.Name)
                .FirstOrDefaultAsync(cancellationToken),
            UsersCount = await _db.Users.CountAsync(x => x.InstitutionId == institutionId, cancellationToken),
            StudentsCount = await _db.Students.CountAsync(x => x.InstitutionId == institutionId, cancellationToken),
            ParentsCount = await _db.Parents.CountAsync(x => x.InstitutionId == institutionId, cancellationToken),
            ExamsCount = await _db.Exams.CountAsync(x => x.InstitutionId == institutionId, cancellationToken),
            AttemptsCount = await _db.Attempts.CountAsync(x => x.InstitutionId == institutionId, cancellationToken),
            RegistrationsCount = await _db.Registrations.CountAsync(x => x.InstitutionId == institutionId, cancellationToken)
        };
    }

    public async Task<ExamAnalyticsDto> GetExamAnalyticsAsync(Guid institutionId, Guid examId, CancellationToken cancellationToken = default)
    {
        var exam = await _db.Exams
            .Include(x => x.Questions)
            .Include(x => x.Registrations)
            .Include(x => x.Attempts)
            .FirstOrDefaultAsync(x => x.Id == examId && x.InstitutionId == institutionId, cancellationToken)
            ?? throw new InvalidOperationException("Exam not found");

        return new ExamAnalyticsDto
        {
            ExamId = exam.Id,
            AllowStudentExit = exam.AllowStudentExit,
            EnableAntiCheat = exam.EnableAntiCheat,
            MaxViolationCount = exam.MaxViolationCount,
            Title = exam.Title,
            ExamCode = exam.ExamCode,
            QuestionsCount = exam.Questions.Count,
            RegisteredStudentsCount = exam.Registrations.Count,
            AttemptedStudentsCount = exam.Attempts.Count,
            LessThan50 = exam.Attempts.Count(x => x.Percentage < 50),
            From50To75 = exam.Attempts.Count(x => x.Percentage >= 50 && x.Percentage <= 75),
            From75To85 = exam.Attempts.Count(x => x.Percentage > 75 && x.Percentage <= 85),
            GreaterThan85 = exam.Attempts.Count(x => x.Percentage > 85)
        };
    }
}
