namespace QuizSystem.Application.DTOs;
public class UploadQuestionsResultDto
{
    public int Inserted { get; set; }
    public int Skipped { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class UploadStudentsResultDto
{
    public int Inserted { get; set; }
    public int Skipped { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class UploadRegistrationsResultDto
{
    public int Inserted { get; set; }
    public int Skipped { get; set; }
    public List<string> Errors { get; set; } = new();
}
