using Microsoft.EntityFrameworkCore;
using QuizSystem.Application.Contracts.Attempts;
using QuizSystem.Application.Contracts.Auth;
using QuizSystem.Application.Contracts.Exams;
using QuizSystem.Application.Contracts.Imports;
using QuizSystem.Application.Contracts.Portals;
using QuizSystem.Application.Contracts.Reports;
using QuizSystem.Application.DTOs;
using QuizSystem.Domain.Entities;
using QuizSystem.Domain.Enums;
using QuizSystem.Infrastructure.Persistence;

namespace QuizSystem.Infrastructure.Services.Attempts;
public class AttemptManagementService : IAttemptManagementService
{
    private static string GetAttemptDisplayStatus(ExamAttempt attempt)
    {
        if (attempt.Status == ExamAttemptStatus.ClosedForViolation)
            return "ClosedForViolation";

        if (attempt.SubmittedAtUtc.HasValue || attempt.Status == ExamAttemptStatus.Submitted)
            return "Submitted";

        return "Started";
    }
    private readonly AppDbContext _db;

    public AttemptManagementService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<AttemptListItemDto>> GetExamAttemptsAsync(Guid examId, CancellationToken cancellationToken = default)
    {
        return await _db.Attempts
            .Where(x => x.ExamId == examId)
            .Include(x => x.StudentProfile)
            .Include(x => x.Exam)
            .OrderByDescending(x => x.StartedAtUtc)
            .Select(x => new AttemptListItemDto
            {
                AttemptId = x.Id,
                StudentId = x.StudentProfileId,
                StudentName = x.StudentProfile.FullName,
                StudentCode = x.StudentProfile.StudentCode,
                ExamId = x.ExamId,
                ExamTitle = x.Exam.Title,
                Score = x.Score ?? 0,
                TotalQuestions = x.TotalQuestions ?? 0,
                Percentage = x.Percentage ?? 0,
                Status = x.Status == ExamAttemptStatus.ClosedForViolation
                    ? "ClosedForViolation"
                    : x.SubmittedAtUtc != null || x.Status == ExamAttemptStatus.Submitted
                        ? "Submitted"
                        : "Started",
                StartedAtUtc = x.StartedAtUtc,
                SubmittedAtUtc = x.SubmittedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<AttemptDetailsDto> GetAttemptDetailsAsync(Guid attemptId, CancellationToken cancellationToken = default)
    {
        var attempt = await _db.Attempts
            .Include(x => x.StudentProfile)
            .Include(x => x.Exam)
            .Include(x => x.QuestionSnapshots)
            .FirstOrDefaultAsync(x => x.Id == attemptId, cancellationToken)
            ?? throw new InvalidOperationException("Attempt not found");

        return new AttemptDetailsDto
        {
            AttemptId = attempt.Id,
            StudentId = attempt.StudentProfileId,
            StudentName = attempt.StudentProfile.FullName,
            StudentCode = attempt.StudentProfile.StudentCode,
            ExamId = attempt.ExamId,
            ExamTitle = attempt.Exam.Title,
            ExamCode = attempt.Exam.ExamCode,
            Score = attempt.Score ?? 0,
            TotalQuestions = attempt.TotalQuestions ?? 0,
            Percentage = attempt.Percentage ?? 0,
            Status = GetAttemptDisplayStatus(attempt),
            StartedAtUtc = attempt.StartedAtUtc,
            SubmittedAtUtc = attempt.SubmittedAtUtc,
            Answers = attempt.QuestionSnapshots
                .OrderBy(x => x.DisplayOrder)
                .Select(q => new AttemptAnswerDto
                {
                    QuestionId = q.OriginalQuestionId,
                    DisplayOrder = q.DisplayOrder,
                    QuestionText = q.QuestionText,
                    QuestionImageUrl = q.QuestionImageUrl,
                    SelectedAnswer = q.SelectedOriginalKey,
                    CorrectAnswer = q.CorrectOriginalKey,
                    IsCorrect = q.IsCorrect == true,
                    Explanation = q.Explanation,
                    Choices = new List<AttemptChoiceDto>
                    {
                        new() { DisplayLabel = q.ChoiceADisplayLabel, OriginalKey = q.ChoiceAOriginalKey, Text = q.ChoiceAText, ImageUrl = q.ChoiceAImageUrl },
                        new() { DisplayLabel = q.ChoiceBDisplayLabel, OriginalKey = q.ChoiceBOriginalKey, Text = q.ChoiceBText, ImageUrl = q.ChoiceBImageUrl },
                        new() { DisplayLabel = q.ChoiceCDisplayLabel, OriginalKey = q.ChoiceCOriginalKey, Text = q.ChoiceCText, ImageUrl = q.ChoiceCImageUrl },
                        new() { DisplayLabel = q.ChoiceDDisplayLabel, OriginalKey = q.ChoiceDOriginalKey, Text = q.ChoiceDText, ImageUrl = q.ChoiceDImageUrl }
                    }
                })
                .ToList()
        };
    }

    public async Task ResetAttemptAsync(Guid attemptId, CancellationToken cancellationToken = default)
    {
        var attempt = await _db.Attempts
            .Include(x => x.Answers)
            .FirstOrDefaultAsync(x => x.Id == attemptId, cancellationToken)
            ?? throw new InvalidOperationException("Attempt not found");

        _db.AttemptAnswers.RemoveRange(attempt.Answers);
        _db.Attempts.Remove(attempt);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
