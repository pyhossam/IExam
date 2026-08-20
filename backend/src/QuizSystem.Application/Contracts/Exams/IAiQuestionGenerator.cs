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

    Task<List<GeneratedCloDto>> GenerateClosAsync(
        string courseName,
        string summarizedEducationalContent,
        int count,
        CancellationToken cancellationToken = default);
}

public sealed record GeneratedCloDto(
    string Code,
    string Description,
    string Domain,
    string CognitiveLevel,
    decimal TargetPercentage,
    int DisplayOrder);
