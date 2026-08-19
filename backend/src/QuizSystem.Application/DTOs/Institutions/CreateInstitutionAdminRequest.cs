namespace QuizSystem.Application.DTOs.Institutions;

public class CreateInstitutionAdminRequest
{
    public Guid InstitutionId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
