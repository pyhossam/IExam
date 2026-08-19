using QuizSystem.Domain.Common;

namespace QuizSystem.Domain.Entities;

public class TeacherSubject : BaseEntity
{
    public Guid InstitutionId { get; set; }
    public Institution Institution { get; set; } = default!;
    public Guid TeacherProfileId { get; set; }
    public TeacherProfile TeacherProfile { get; set; } = default!;
    public Guid SubjectId { get; set; }
    public Subject Subject { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}
