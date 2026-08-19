using QuizSystem.Domain.Common;

namespace QuizSystem.Domain.Entities;

public class ExamAttemptQuestionSnapshot : BaseEntity
{
    public Guid InstitutionId { get; set; }
    public Institution Institution { get; set; } = default!;

    public Guid ExamAttemptId { get; set; }
    public ExamAttempt ExamAttempt { get; set; } = default!;

    public Guid OriginalQuestionId { get; set; }

    public int DisplayOrder { get; set; }

    public string QuestionText { get; set; } = string.Empty;
    public string? QuestionImageUrl { get; set; }

    public string ChoiceADisplayLabel { get; set; } = "A";
    public string ChoiceAOriginalKey { get; set; } = "A";
    public string ChoiceAText { get; set; } = string.Empty;
    public string? ChoiceAImageUrl { get; set; }

    public string ChoiceBDisplayLabel { get; set; } = "B";
    public string ChoiceBOriginalKey { get; set; } = "B";
    public string ChoiceBText { get; set; } = string.Empty;
    public string? ChoiceBImageUrl { get; set; }

    public string ChoiceCDisplayLabel { get; set; } = "C";
    public string ChoiceCOriginalKey { get; set; } = "C";
    public string ChoiceCText { get; set; } = string.Empty;
    public string? ChoiceCImageUrl { get; set; }

    public string ChoiceDDisplayLabel { get; set; } = "D";
    public string ChoiceDOriginalKey { get; set; } = "D";
    public string ChoiceDText { get; set; } = string.Empty;
    public string? ChoiceDImageUrl { get; set; }

    public string CorrectOriginalKey { get; set; } = string.Empty;
    public string? Explanation { get; set; }

    public string? SelectedOriginalKey { get; set; }
    public bool? IsCorrect { get; set; }
}
