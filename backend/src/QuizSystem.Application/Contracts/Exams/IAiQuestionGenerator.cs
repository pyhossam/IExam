using QuizSystem.Application.DTOs;

namespace QuizSystem.Application.Contracts.Exams;
public interface IAiQuestionGenerator
{
    Task<string> SummarizeEducationalContentAsync(
        string educationalContent,
        string? examContext = null,
        CancellationToken cancellationToken = default);

    Task<List<GeneratedQuestionDto>> GenerateQuestionsAsync(
        string topic,
        int count,
        string? educationalContent = null,
        string? blueprintInstructions = null,
        CancellationToken cancellationToken = default);
}
