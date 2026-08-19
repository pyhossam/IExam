using QuizSystem.Domain.Common;
using QuizSystem.Domain.Enums;

namespace QuizSystem.Domain.Entities;
public class AppUser : BaseEntity
{
    public string UserName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Email { get; set; }
    public bool MustChangePassword { get; set; }
    public string? PasswordResetTokenHash { get; set; }
    public DateTime? PasswordResetTokenExpiresAtUtc { get; set; }
    public string? EmailVerificationTokenHash { get; set; }
    public DateTime? EmailVerificationTokenExpiresAtUtc { get; set; }
    public DateTime? EmailVerifiedAtUtc { get; set; }

    // Null only for platform-level SuperAdmin. All tenant users must have InstitutionId.
    public Guid? InstitutionId { get; set; }
    public Institution? Institution { get; set; }

    public Guid? StudentProfileId { get; set; }
    public StudentProfile? StudentProfile { get; set; }

    public Guid? ParentProfileId { get; set; }
    public ParentProfile? ParentProfile { get; set; }

    public Guid? TeacherProfileId { get; set; }
    public TeacherProfile? TeacherProfile { get; set; }
}
