using QuizSystem.Domain.Common;

namespace QuizSystem.Domain.Entities;

public class TeacherProfile : BaseEntity
{
    public Guid InstitutionId { get; set; }
    public Institution Institution { get; set; } = default!;
    public Guid? UserId { get; set; }
    public AppUser? User { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string TeacherCode { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<ClassSection> ClassSections { get; set; } = new List<ClassSection>();
    public ICollection<TeacherSubject> TeacherSubjects { get; set; } = new List<TeacherSubject>();
}
