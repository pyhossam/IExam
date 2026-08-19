namespace QuizSystem.Application.DTOs;

public class LookupItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Grade { get; set; }

}
public class ParentLookupResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }
    public string? UserName { get; set; }

    public int ChildrenCount { get; set; }
}
