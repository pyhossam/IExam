using QuizSystem.Domain.Common;

namespace QuizSystem.Domain.Entities;

public class SectionStudent : BaseEntity
{
    public Guid InstitutionId { get; set; }
    public Institution Institution { get; set; } = default!;
    public Guid ClassSectionId { get; set; }
    public ClassSection ClassSection { get; set; } = default!;
    public Guid StudentProfileId { get; set; }
    public StudentProfile StudentProfile { get; set; } = default!;
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
