using QuizSystem.Domain.Common;

namespace QuizSystem.Domain.Entities;
public class ExamRegistration : BaseEntity
{
    public Guid InstitutionId { get; set; }
    public Institution Institution { get; set; } = default!;

    public Guid? ClassSectionId { get; set; }
    public ClassSection? ClassSection { get; set; }

    public Guid ExamId { get; set; }
    public Exam Exam { get; set; } = default!;

    public Guid StudentProfileId { get; set; }
    public StudentProfile StudentProfile { get; set; } = default!;

    public Guid AssignedByUserId { get; set; }
    public bool IsActive { get; set; } = true;
}
