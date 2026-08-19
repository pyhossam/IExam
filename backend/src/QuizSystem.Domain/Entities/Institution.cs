using QuizSystem.Domain.Common;
using QuizSystem.Domain.Enums;

namespace QuizSystem.Domain.Entities;

public class Institution : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public ExamManagementMode ExamManagementMode { get; set; } = ExamManagementMode.TeachersAndExamSupervisors;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
    public ICollection<StudentProfile> Students { get; set; } = new List<StudentProfile>();
    public ICollection<ParentProfile> Parents { get; set; } = new List<ParentProfile>();
    public ICollection<TeacherProfile> Teachers { get; set; } = new List<TeacherProfile>();
    public ICollection<GradeLevel> GradeLevels { get; set; } = new List<GradeLevel>();
    public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
    public ICollection<ClassSection> ClassSections { get; set; } = new List<ClassSection>();
}
