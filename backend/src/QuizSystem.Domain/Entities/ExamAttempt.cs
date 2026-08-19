using QuizSystem.Domain.Common;
using QuizSystem.Domain.Enums;

namespace QuizSystem.Domain.Entities;
public class ExamAttempt : BaseEntity
{
    public Guid InstitutionId { get; set; }
    public Institution Institution { get; set; } = default!;

    public Guid? ClassSectionId { get; set; }
    public ClassSection? ClassSection { get; set; }

    public Guid ExamId { get; set; }
    public Exam Exam { get; set; } = default!;

    public Guid StudentProfileId { get; set; }
    public StudentProfile StudentProfile { get; set; } = default!;
    public int AttemptNumber { get; set; } = 1;

    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAtUtc { get; set; }

    public int? Score { get; set; }
    public int? TotalQuestions { get; set; }
    public int? Percentage { get; set; }
    public ExamAttemptStatus Status { get; set; } = ExamAttemptStatus.Started;

    public ICollection<AttemptAnswer> Answers { get; set; } = new List<AttemptAnswer>();
    public ICollection<ExamAttemptDraftAnswer> DraftAnswers { get; set; } = new List<ExamAttemptDraftAnswer>();
    public ICollection<ExamAttemptViolation> Violations { get; set; } = new List<ExamAttemptViolation>();
    public ICollection<ExamAttemptQuestionSnapshot> QuestionSnapshots { get; set; } = new List<ExamAttemptQuestionSnapshot>();
}
