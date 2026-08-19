namespace QuizSystem.Application.DTOs;

public class ParentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public string PhoneNumber { get; set; }

    public int ChildrenCount { get; set; }

    public List<StudentMiniDto> Students { get; set; } = new();
}
