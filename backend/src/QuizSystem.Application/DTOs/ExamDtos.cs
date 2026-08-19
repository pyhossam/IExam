namespace QuizSystem.Application.DTOs;
public class CreateAiExamRequest
{
    public Guid? SubjectId { get; set; }
    public string AssessmentType { get; set; } = "General";
    public int MaxAttempts { get; set; } = 1;
    public string Title { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ExamCode { get; set; } = string.Empty;
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public int BankQuestionCount { get; set; }
    public int ExamQuestionCount { get; set; }
}

public class CreateManualExamRequest
{
    public Guid? SubjectId { get; set; }
    public string AssessmentType { get; set; } = "General";
    public int MaxAttempts { get; set; } = 1;
    public string Title { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ExamCode { get; set; } = string.Empty;
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public int ExamQuestionCount { get; set; }
}

public class UpdateExamSettingsRequest
{
    public Guid? SubjectId { get; set; }
    public string AssessmentType { get; set; } = "General";
    public int MaxAttempts { get; set; } = 1;
    public string Title { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public int BankQuestionCount { get; set; }
    public int ExamQuestionCount { get; set; }
    public Dictionary<string, int> BlueprintCloDistribution { get; set; } = new();
    public Dictionary<string, int> BlueprintBloomDistribution { get; set; } = new();
    public bool IsPublished { get; set; }
    public bool AllowStudentExit { get; set; }
    public bool EnableAntiCheat { get; set; } = true;
    public int MaxViolationCount { get; set; } = 3; 
}

public class UpsertExamQuestionRequest
{
    public Guid? CourseLearningOutcomeId { get; set; }
    public string CognitiveLevel { get; set; } = "Understand";
    public string QuestionText { get; set; } = string.Empty;

    public string? QuestionImageUrl { get; set; }
    public string? ChoiceAImageUrl { get; set; }
    public string? ChoiceBImageUrl { get; set; }
    public string? ChoiceCImageUrl { get; set; }
    public string? ChoiceDImageUrl { get; set; }

    public string ChoiceA { get; set; } = string.Empty;
    public string ChoiceB { get; set; } = string.Empty;
    public string ChoiceC { get; set; } = string.Empty;
    public string ChoiceD { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = "A";
    public string? Explanation { get; set; }
}

public class GeneratedQuestionDto
{
    public string? CloCode { get; set; }
    public string CognitiveLevel { get; set; } = "Understand";
    public string QuestionText { get; set; } = string.Empty;

    public string? QuestionImageUrl { get; set; }
    public string? ChoiceAImageUrl { get; set; }
    public string? ChoiceBImageUrl { get; set; }
    public string? ChoiceCImageUrl { get; set; }
    public string? ChoiceDImageUrl { get; set; }

    public string ChoiceA { get; set; } = string.Empty;
    public string ChoiceB { get; set; } = string.Empty;
    public string ChoiceC { get; set; } = string.Empty;
    public string ChoiceD { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = "A";
    public string? Explanation { get; set; }
}

public class ExamQuestionDto
{
    public Guid? CourseLearningOutcomeId { get; set; }
    public string? CloCode { get; set; }
    public string CognitiveLevel { get; set; } = "Understand";
    public Guid Id { get; set; }
    public string QuestionText { get; set; } = string.Empty;

    public string? QuestionImageUrl { get; set; }
    public string? ChoiceAImageUrl { get; set; }
    public string? ChoiceBImageUrl { get; set; }
    public string? ChoiceCImageUrl { get; set; }
    public string? ChoiceDImageUrl { get; set; }

    public string ChoiceA { get; set; } = string.Empty;
    public string ChoiceB { get; set; } = string.Empty;
    public string ChoiceC { get; set; } = string.Empty;
    public string ChoiceD { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = string.Empty;
    public string? Explanation { get; set; }
}

public class ExamListItemDto
{
    public Guid? SubjectId { get; set; }
    public string AssessmentType { get; set; } = "General";
    public int MaxAttempts { get; set; } = 1;
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string ExamCode { get; set; } = string.Empty;
    public int BankQuestionCount { get; set; }
    public int ExamQuestionCount { get; set; }
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public bool CreatedManually { get; set; }
    public bool IsPublished { get; set; }
    public bool AllowStudentExit { get; set; }
    public bool EnableAntiCheat { get; set; } = true;
    public int MaxViolationCount { get; set; } = 3; 
    public Guid? InstitutionId { get; set; }
    public string? InstitutionName { get; set; }
    public int RegisteredStudentsCount { get; set; }
    public int AttemptsCount { get; set; }
}

public class ExamDetailsDto
{
    public Guid? SubjectId { get; set; }
    public string? SubjectName { get; set; }
    public string? SubjectCode { get; set; }
    public string AssessmentType { get; set; } = "General";
    public int MaxAttempts { get; set; } = 1;
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ExamCode { get; set; } = string.Empty;
    public int BankQuestionCount { get; set; }
    public int ExamQuestionCount { get; set; }
    public Dictionary<string, int> BlueprintCloDistribution { get; set; } = new();
    public Dictionary<string, int> BlueprintBloomDistribution { get; set; } = new();
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public bool CreatedManually { get; set; }
    public bool IsPublished { get; set; }
    public bool AllowStudentExit { get; set; }
    public bool EnableAntiCheat { get; set; } = true;
    public int MaxViolationCount { get; set; } = 3; 
    public Guid? InstitutionId { get; set; }
    public string? InstitutionName { get; set; }
    public List<ExamQuestionDto> Questions { get; set; } = new();
}

public class StartExamRequest
{
    public Guid ExamId { get; set; }
}


public class DashboardOverviewDto
{
    public string Role { get; set; } = string.Empty;
    public List<AssignedCourseDto> AssignedCourses { get; set; } = new();
    public string? InstitutionName { get; set; }
    public int UsersCount { get; set; }
    public int StudentsCount { get; set; }
    public int ParentsCount { get; set; }
    public int ExamsCount { get; set; }
    public int AttemptsCount { get; set; }
    public int RegistrationsCount { get; set; }
}

public class AssignedCourseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int ClosCount { get; set; }
    public int ExamsCount { get; set; }
}

public class ExamAnalyticsDto
{
    public Guid ExamId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ExamCode { get; set; } = string.Empty;
    public bool AllowStudentExit { get; set; }
    public bool EnableAntiCheat { get; set; } = true;
    public int MaxViolationCount { get; set; } = 3;
    public int QuestionsCount { get; set; }
    public int RegisteredStudentsCount { get; set; }
    public int AttemptedStudentsCount { get; set; }
    public int LessThan50 { get; set; }
    public int From50To75 { get; set; }
    public int From75To85 { get; set; }
    public int GreaterThan85 { get; set; }
}

public class LeaderboardItemDto
{
    public int Rank { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public int Score { get; set; }
    public int TotalQuestions { get; set; }
    public int Percentage { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
}
