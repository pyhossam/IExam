using QuizSystem.Domain.Common;

namespace QuizSystem.Domain.Entities;
public class ParentStudentLink : BaseEntity
{
    public Guid InstitutionId { get; set; }
    public Institution Institution { get; set; } = default!;

    public Guid ParentProfileId { get; set; }
    public ParentProfile ParentProfile { get; set; } = default!;

    public Guid StudentProfileId { get; set; }
    public StudentProfile StudentProfile { get; set; } = default!;
}
