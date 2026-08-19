using QuizSystem.Application.DTOs;

namespace QuizSystem.Application.Contracts.Attempts;
public interface IStudentExamService
{
    Task<List<object>> GetAvailableExamsForStudentAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<StartExamResponse> StartExamAsync(Guid studentId, Guid examId, CancellationToken cancellationToken = default);
    Task<ExamResultResponse> SubmitExamAsync(Guid studentId, SubmitExamRequest request, CancellationToken cancellationToken = default);
    Task<List<ParentChildResultResponse>> GetChildrenResultsAsync(Guid parentProfileId, CancellationToken cancellationToken = default);
}
