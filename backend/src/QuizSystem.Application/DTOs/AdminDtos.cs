namespace QuizSystem.Application.DTOs;
public class CreateUserRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class CreateStudentRequest
{
    public string FullName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public string? Branch { get; set; }
    public string? NationalId { get; set; }
    public string? Mobile { get; set; }
    public string? Nationality { get; set; }
    public string? ImagePath { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
}

public class CreateParentRequest
{
    public string FullName { get; set; } = string.Empty;
    public string ParentCode { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public List<Guid> StudentIds { get; set; } = new();
    public string? UserName { get; set; }
    public string? Password { get; set; }
}

public class CreateExamRequest
{
    public string Title { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ExamCode { get; set; } = string.Empty;
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public int BankQuestionCount { get; set; }
    public int ExamQuestionCount { get; set; }
    public bool CreatedManually { get; set; }
    public bool AllowStudentExit { get; set; }
    public bool EnableAntiCheat { get; set; } = true;
    public int MaxViolationCount { get; set; } = 3; 
}

public class AddQuestionRequest
{
    public string QuestionText { get; set; } = string.Empty;
    public string ChoiceA { get; set; } = string.Empty;
    public string ChoiceB { get; set; } = string.Empty;
    public string ChoiceC { get; set; } = string.Empty;
    public string ChoiceD { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = "A";
    public string? Explanation { get; set; }
}

public class RegisterStudentRequest
{
    public Guid StudentId { get; set; }
}

public class DashboardResponse
{
    public string? InstitutionName { get; set; }
    public int Users { get; set; }
    public int Students { get; set; }
    public int Parents { get; set; }
    public int Exams { get; set; }
    public int Attempts { get; set; }
}
