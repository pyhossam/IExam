namespace QuizSystem.Application.DTOs;

public class CreateUserManagementRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    public Guid? StudentProfileId { get; set; }
    public Guid? ParentProfileId { get; set; }
    public Guid? TeacherProfileId { get; set; }
}

public class UpdateUserManagementRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public Guid? StudentProfileId { get; set; }
    public Guid? ParentProfileId { get; set; }
    public Guid? TeacherProfileId { get; set; }
}

public class UserListItemDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? Email { get; set; }
    public bool MustChangePassword { get; set; }

    public Guid? InstitutionId { get; set; }
    public string? InstitutionName { get; set; }

    public Guid? StudentProfileId { get; set; }
    public string? StudentName { get; set; }

    public Guid? ParentProfileId { get; set; }
    public string? ParentName { get; set; }
    public Guid? TeacherProfileId { get; set; }
    public string? TeacherName { get; set; }
}

public sealed class AdminResetPasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
}
