using QuizSystem.Application.DTOs;

namespace QuizSystem.Application.Contracts.Attempts;
public interface IAttemptManagementService
{
    Task<List<AttemptListItemDto>> GetExamAttemptsAsync(Guid examId, CancellationToken cancellationToken = default);
    Task<AttemptDetailsDto> GetAttemptDetailsAsync(Guid attemptId, CancellationToken cancellationToken = default);
    Task ResetAttemptAsync(Guid attemptId, CancellationToken cancellationToken = default);
}
