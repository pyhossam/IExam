using QuizSystem.Domain.Common;
using QuizSystem.Domain.Enums;

namespace QuizSystem.Domain.Entities;
public class ExamQuestion : BaseEntity
{
    public Guid InstitutionId { get; set; }
    public Institution Institution { get; set; } = default!;

    public Guid ExamId { get; set; }
    public Exam Exam { get; set; } = default!;

    public Guid? CourseLearningOutcomeId { get; set; }
    public CourseLearningOutcome? CourseLearningOutcome { get; set; }
    public CognitiveLevel CognitiveLevel { get; set; } = CognitiveLevel.Understand;

    public string QuestionText { get; set; } = string.Empty;

    public string? QuestionImageUrl { get; set; }
    public string ChoiceA { get; set; } = string.Empty;
    public string? ChoiceAImageUrl { get; set; }
    public string ChoiceB { get; set; } = string.Empty;
    public string? ChoiceBImageUrl { get; set; }
    public string ChoiceC { get; set; } = string.Empty;
    public string? ChoiceCImageUrl { get; set; }
    public string ChoiceD { get; set; } = string.Empty;
    public string? ChoiceDImageUrl { get; set; }
    public string CorrectAnswer { get; set; } = "A";
    public string? Explanation { get; set; }
}
