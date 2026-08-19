using QuizSystem.Domain.Common;

namespace QuizSystem.Domain.Entities;

public class ExamAttemptViolation : BaseEntity
{
    public Guid InstitutionId { get; set; }
    public Institution Institution { get; set; } = default!;

    public Guid ExamAttemptId { get; set; }
    public ExamAttempt ExamAttempt { get; set; } = default!;

    public string Type { get; set; } = string.Empty;
    public string? Details { get; set; }

    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
