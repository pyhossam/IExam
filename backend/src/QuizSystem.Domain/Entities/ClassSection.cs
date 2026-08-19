using QuizSystem.Domain.Common;

namespace QuizSystem.Domain.Entities;

public class ClassSection : BaseEntity
{
    public Guid InstitutionId { get; set; }
    public Institution Institution { get; set; } = default!;
    public Guid GradeLevelId { get; set; }
    public GradeLevel GradeLevel { get; set; } = default!;
    public Guid SubjectId { get; set; }
    public Subject Subject { get; set; } = default!;
    public Guid? TeacherProfileId { get; set; }
    public TeacherProfile? TeacherProfile { get; set; }
    public string Name { get; set; } = string.Empty;
    public string GenderType { get; set; } = "Mixed";
    public string? AcademicYear { get; set; }
    public string? Term { get; set; }
    public int? Capacity { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<SectionStudent> SectionStudents { get; set; } = new List<SectionStudent>();
}
