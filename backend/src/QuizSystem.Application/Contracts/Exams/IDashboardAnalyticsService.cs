using QuizSystem.Application.DTOs;

namespace QuizSystem.Application.Contracts.Exams;
public interface IDashboardAnalyticsService
{
    Task<DashboardOverviewDto> GetOverviewAsync(Guid institutionId, CancellationToken cancellationToken = default);
    Task<ExamAnalyticsDto> GetExamAnalyticsAsync(Guid institutionId, Guid examId, CancellationToken cancellationToken = default);
}
