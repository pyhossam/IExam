using Microsoft.AspNetCore.Http;
using QuizSystem.Application.DTOs;

namespace QuizSystem.Application.Contracts.Exams;
public interface IExamManagementService
{
    Task<Guid> CreateAiExamAsync(Guid institutionId, Guid createdByUserId, CreateAiExamRequest request, CancellationToken cancellationToken = default);
    Task<Guid> CreateManualExamAsync(Guid institutionId, Guid createdByUserId, CreateManualExamRequest request, CancellationToken cancellationToken = default);

    Task<List<ExamListItemDto>> GetExamsAsync(Guid? institutionId, bool isSuperAdmin, CancellationToken cancellationToken = default);
    Task<ExamDetailsDto> GetExamDetailsAsync(Guid? institutionId, bool isSuperAdmin, Guid examId, CancellationToken cancellationToken = default);

    Task UpdateExamSettingsAsync(Guid examId, UpdateExamSettingsRequest request, CancellationToken cancellationToken = default);

    Task<Guid> AddQuestionAsync(Guid examId, UpsertExamQuestionRequest request, CancellationToken cancellationToken = default);
    Task UpdateQuestionAsync(Guid questionId, UpsertExamQuestionRequest request, CancellationToken cancellationToken = default);
    Task DeleteQuestionAsync(Guid questionId, CancellationToken cancellationToken = default);

    Task<UploadQuestionsResultDto> UploadQuestionsAsync(Guid examId, IFormFile file, CancellationToken cancellationToken = default);

    Task<byte[]> BuildQuestionsTemplateAsync(Guid examId, CancellationToken cancellationToken = default);
}
