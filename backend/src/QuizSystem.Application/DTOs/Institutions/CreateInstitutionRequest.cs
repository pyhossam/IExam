namespace QuizSystem.Application.DTOs.Institutions;

public class CreateInstitutionRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? LogoUrl { get; set; }
}
