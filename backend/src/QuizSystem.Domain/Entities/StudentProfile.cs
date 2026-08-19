using QuizSystem.Domain.Common;

namespace QuizSystem.Domain.Entities;
public class StudentProfile : BaseEntity
{
    public Guid InstitutionId { get; set; }
    public Institution Institution { get; set; } = default!;

    public string FullName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public string? Branch { get; set; }
    public string NationalId { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string? Nationality { get; set; }
    public string? ImagePath { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public Guid? GradeLevelId { get; set; }
    public GradeLevel? GradeLevel { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<ParentStudentLink> ParentLinks { get; set; } = new List<ParentStudentLink>();
    public ICollection<ExamRegistration> ExamRegistrations { get; set; } = new List<ExamRegistration>();
    public ICollection<ExamAttempt> ExamAttempts { get; set; } = new List<ExamAttempt>();
    public ICollection<SectionStudent> SectionStudents { get; set; } = new List<SectionStudent>();
}
