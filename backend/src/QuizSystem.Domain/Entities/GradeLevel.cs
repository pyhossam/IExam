using QuizSystem.Domain.Common;

namespace QuizSystem.Domain.Entities;

public class GradeLevel : BaseEntity
{
    public Guid InstitutionId { get; set; }
    public Institution Institution { get; set; } = default!;
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
    public ICollection<ClassSection> ClassSections { get; set; } = new List<ClassSection>();
}
