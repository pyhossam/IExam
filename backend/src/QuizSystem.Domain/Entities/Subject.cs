using QuizSystem.Domain.Common;

namespace QuizSystem.Domain.Entities;

public class Subject : BaseEntity
{
    public Guid InstitutionId { get; set; }
    public Institution Institution { get; set; } = default!;
    public Guid GradeLevelId { get; set; }
    public GradeLevel GradeLevel { get; set; } = default!;
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<ClassSection> ClassSections { get; set; } = new List<ClassSection>();
    public ICollection<TeacherSubject> TeacherSubjects { get; set; } = new List<TeacherSubject>();
}
