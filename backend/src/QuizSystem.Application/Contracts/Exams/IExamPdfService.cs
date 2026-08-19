using QuizSystem.Application.DTOs;

namespace QuizSystem.Application.Contracts.Exams;
public interface IExamPdfService
{
    Task<byte[]> ExportQuestionsPdfAsync(Guid examId, bool withAnswers, CancellationToken cancellationToken = default);
    Task<byte[]> ExportRandomFormsPdfAsync(Guid examId, int formsCount, CancellationToken cancellationToken = default);
    Task<byte[]> ExportRandomFormsAnswerKeysPdfAsync(Guid examId, int formsCount, CancellationToken cancellationToken = default);
}
