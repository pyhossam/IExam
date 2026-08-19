using QuizSystem.Domain.Common;
using QuizSystem.Domain.Enums;

namespace QuizSystem.Domain.Entities;
public class Exam : BaseEntity
{
    public Guid InstitutionId { get; set; }
    public Institution Institution { get; set; } = default!;

    public Guid? SubjectId { get; set; }
    public Subject? Subject { get; set; }

    public Guid? ClassSectionId { get; set; }
    public ClassSection? ClassSection { get; set; }

    public Guid? TeacherProfileId { get; set; }
    public TeacherProfile? TeacherProfile { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ExamCode { get; set; } = string.Empty;
    public AssessmentType AssessmentType { get; set; } = AssessmentType.General;
    public int MaxAttempts { get; set; } = 1;

    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }

    public int BankQuestionCount { get; set; }
    public int ExamQuestionCount { get; set; }
    public string BlueprintCloDistributionJson { get; set; } = "{}";
    public string BlueprintBloomDistributionJson { get; set; } = "{}";
    public bool CreatedManually { get; set; }
    public bool IsPublished { get; set; }
    public bool AllowStudentExit { get; set; } = true;
    public bool EnableAntiCheat { get; set; } = true;
    public int MaxViolationCount { get; set; } = 3;

    public Guid CreatedByUserId { get; set; }

    public ICollection<ExamQuestion> Questions { get; set; } = new List<ExamQuestion>();
    public ICollection<ExamRegistration> Registrations { get; set; } = new List<ExamRegistration>();
    public ICollection<ExamAttempt> Attempts { get; set; } = new List<ExamAttempt>();
}
