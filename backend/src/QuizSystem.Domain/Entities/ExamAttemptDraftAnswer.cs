using QuizSystem.Domain.Common;

namespace QuizSystem.Domain.Entities;

public class ExamAttemptDraftAnswer : BaseEntity
{
    public Guid InstitutionId { get; set; }
    public Institution Institution { get; set; } = default!;

    public Guid ExamAttemptId { get; set; }
    public ExamAttempt ExamAttempt { get; set; } = default!;

    public Guid QuestionSnapshotId { get; set; }

    public string? SelectedAnswer { get; set; }

    public DateTime SavedAtUtc { get; set; } = DateTime.UtcNow;
}
