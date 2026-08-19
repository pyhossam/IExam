using QuizSystem.Domain.Common;

namespace QuizSystem.Domain.Entities;
public class AttemptAnswer : BaseEntity
{
    public Guid InstitutionId { get; set; }
    public Institution Institution { get; set; } = default!;

    public Guid ExamAttemptId { get; set; }
    public ExamAttempt ExamAttempt { get; set; } = default!;

    public Guid ExamQuestionId { get; set; }
    public ExamQuestion ExamQuestion { get; set; } = default!;

    public string? SelectedAnswer { get; set; }
    public string CorrectAnswer { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public string? Explanation { get; set; }
}
