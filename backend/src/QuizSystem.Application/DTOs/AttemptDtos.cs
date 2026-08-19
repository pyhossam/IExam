namespace QuizSystem.Application.DTOs;

public class SubmitAnswerItem
{
    public Guid QuestionId { get; set; }
    public Guid QuestionSnapshotId { get; set; }
    public string? SelectedAnswer { get; set; }
}

public class SubmitExamRequest
{
    public Guid ExamId { get; set; }
    public List<SubmitAnswerItem>? Answers { get; set; }
    public bool IsAutoSubmitDueToExit { get; set; } = new();
}

public class ExamChoiceView
{
    public string DisplayLabel { get; set; } = string.Empty;   // A/B/C/D shown to student
    public string OriginalKey { get; set; } = string.Empty;    // A/B/C/D original answer key
    public string Text { get; set; } = string.Empty;
}

public class ExamQuestionView
{
    public Guid Id { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string? QuestionImageUrl { get; set; }
    public List<ExamChoiceView> Choices { get; set; } = new();
}

public class StartExamResponse
{
    public Guid AttemptId { get; set; }
    public Guid ExamId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ExamCode { get; set; } = string.Empty;
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    
    public bool AllowStudentExit { get; set; }
    public bool EnableAntiCheat { get; set; } = true;
    public int MaxViolationCount { get; set; } = 3;
public List<ExamQuestionView> Questions { get; set; } = new();
}

public class ExamResultResponse
{
    public Guid AttemptId { get; set; }
    public Guid ExamId { get; set; }
    public int Score { get; set; }
    public int TotalQuestions { get; set; }
    public int Percentage { get; set; }
}


