namespace QuizSystem.Application.DTOs;
public class StudentPortalExamItemDto
{
    public Guid ExamId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ExamCode { get; set; } = string.Empty;
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public string AvailabilityStatus { get; set; } = string.Empty;
    public bool CanStart { get; set; }
    public bool HasSubmitted { get; set; }

    public int? Score { get; set; }
    public int? TotalQuestions { get; set; }
    public decimal? Percentage { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }

}

public class StudentPortalDashboardDto
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public List<StudentPortalExamItemDto> AvailableExams { get; set; } = new();
    public List<AttemptListItemDto> AttemptHistory { get; set; } = new();
}

public class ParentChildSummaryDto
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
}
public class AttemptListItemDto
{
    public Guid AttemptId { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public Guid ExamId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public int Score { get; set; }
    public int TotalQuestions { get; set; }
    public int Percentage { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
}
public class AttemptDetailsDto
{
    public Guid AttemptId { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public Guid ExamId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public string ExamCode { get; set; } = string.Empty;
    public int Score { get; set; }
    public int TotalQuestions { get; set; }
    public int Percentage { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public List<AttemptAnswerDto> Answers { get; set; } = new();
}
public class AttemptAnswerDto
{
    public Guid QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string? QuestionImageUrl { get; set; }
    public string? SelectedAnswer { get; set; }
    public string CorrectAnswer { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public string? Explanation { get; set; }
    public int DisplayOrder { get; set; }
    public List<AttemptChoiceDto> Choices { get; set; } = new();
}
public class AttemptChoiceDto
{
    public string DisplayLabel { get; set; } = string.Empty;
    public string OriginalKey { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}
public class ParentChildResultResponse
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public string ExamTitle { get; set; } = string.Empty;
    public string ExamCode { get; set; } = string.Empty;
    public int Score { get; set; }
    public int TotalQuestions { get; set; }
    public int Percentage { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
        public bool IsSubmitted { get; set; }
}

public class ParentPortalDashboardDto
{
    public Guid ParentId { get; set; }
    public string ParentName { get; set; } = string.Empty;
    public string ParentCode { get; set; } = string.Empty;
    public List<ParentChildSummaryDto> Children { get; set; } = new();
    public List<ParentChildResultResponse> ChildrenResults { get; set; } = new();
}
