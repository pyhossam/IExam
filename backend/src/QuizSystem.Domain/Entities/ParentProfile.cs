using QuizSystem.Domain.Common;

namespace QuizSystem.Domain.Entities;
public class ParentProfile : BaseEntity
{
    public Guid InstitutionId { get; set; }
    public Institution Institution { get; set; } = default!;

    public string FullName { get; set; } = string.Empty;
    public string ParentCode { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Guid? UserId { get; set; }
    public AppUser? User { get; set; }
    public ICollection<ParentStudentLink> ParentStudentLinks { get; set; } = new List<ParentStudentLink>();
}
