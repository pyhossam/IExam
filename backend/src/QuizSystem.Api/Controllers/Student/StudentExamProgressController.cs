using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizSystem.Domain.Entities;
using QuizSystem.Domain.Enums;
using QuizSystem.Infrastructure.Persistence;
using System.Security.Claims;

namespace QuizSystem.Api.Controllers.Student;

[ApiController]
[Route("api/student/exams")]
[Authorize(Roles = "Student")]
public class StudentExamProgressController : ControllerBase
{
    private readonly AppDbContext _db;

    public StudentExamProgressController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("{examId:guid}/draft")]
    public async Task<IActionResult> SaveDraft(
        Guid examId,
        [FromBody] SaveExamDraftRequest request,
        CancellationToken cancellationToken)
    {
        var studentId = GetStudentProfileId();

        var attempt = await _db.Set<ExamAttempt>()
            .AsNoTracking()
            .Where(x =>
                x.ExamId == examId &&
                x.StudentProfileId == studentId &&
                x.Status == ExamAttemptStatus.Started &&
                x.SubmittedAtUtc == null)
            .Select(x => new { x.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (attempt is null)
            return NotFound(new ProblemDetails
            {
                Title = "Attempt not found",
                Detail = "لا توجد محاولة نشطة لهذا الاختبار",
                Status = StatusCodes.Status404NotFound
            });

        var validSnapshotIds = await _db.Set<ExamAttemptQuestionSnapshot>()
            .AsNoTracking()
            .Where(x => x.ExamAttemptId == attempt.Id)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var saved = await UpsertDraftAnswersDirectAsync(
            attempt.Id,
            validSnapshotIds.ToHashSet(),
            request.Answers ?? new List<SaveExamDraftAnswerItem>(),
            cancellationToken);

        var totalDraftAnswers = await _db.Set<ExamAttemptDraftAnswer>()
            .CountAsync(x => x.ExamAttemptId == attempt.Id, cancellationToken);

        return Ok(new
        {
            attemptId = attempt.Id,
            savedCount = saved,
            totalDraftAnswers,
            savedAtUtc = DateTime.UtcNow
        });
    }

    [HttpPost("{examId:guid}/violation")]
    public async Task<IActionResult> RegisterViolation(
        Guid examId,
        [FromBody] RegisterExamViolationRequest request,
        CancellationToken cancellationToken)
    {
        var studentId = GetStudentProfileId();

        var attempt = await _db.Set<ExamAttempt>()
            .AsNoTracking()
            .Where(x =>
                x.ExamId == examId &&
                x.StudentProfileId == studentId &&
                x.Status == ExamAttemptStatus.Started &&
                x.SubmittedAtUtc == null)
            .Select(x => new { x.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (attempt is null)
        {
            return Ok(new
            {
                attemptClosed = true,
                closedForViolation = true,
                status = "ClosedForViolation",
                message = "تم غلق الاختبار أو لا توجد محاولة نشطة."
            });
        }

        var snapshots = await _db.Set<ExamAttemptQuestionSnapshot>()
            .Where(x => x.ExamAttemptId == attempt.Id)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync(cancellationToken);

        if (request.Answers is not null && request.Answers.Count > 0)
        {
            await UpsertDraftAnswersDirectAsync(
                attempt.Id,
                snapshots.Select(x => x.Id).ToHashSet(),
                request.Answers,
                cancellationToken);
        }

        _db.Set<ExamAttemptViolation>().Add(new ExamAttemptViolation
        {
            Id = Guid.NewGuid(),
            ExamAttemptId = attempt.Id,
            Type = string.IsNullOrWhiteSpace(request.Type) ? "Unknown" : request.Type.Trim(),
            Details = request.Details,
            OccurredAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);

        var violationsCount = await _db.Set<ExamAttemptViolation>()
            .CountAsync(x => x.ExamAttemptId == attempt.Id, cancellationToken);

        const int warningAt = 2;
        const int closeAt = 3;

        var shouldWarn = violationsCount >= warningAt && violationsCount < closeAt;
        var shouldClose = violationsCount >= closeAt;

        int? score = null;
        int? totalQuestions = null;
        int? percentage = null;

        if (shouldClose)
        {
            var draftRows = await _db.Set<ExamAttemptDraftAnswer>()
                .AsNoTracking()
                .Where(x =>
                    x.ExamAttemptId == attempt.Id &&
                    !string.IsNullOrWhiteSpace(x.SelectedAnswer))
                .ToListAsync(cancellationToken);

            var draftMap = draftRows
                .GroupBy(x => x.QuestionSnapshotId)
                .ToDictionary(
                    x => x.Key,
                    x => x.OrderByDescending(a => a.SavedAtUtc).First().SelectedAnswer!.Trim().ToUpperInvariant());

            var scoreValue = 0;

            foreach (var snapshot in snapshots)
            {
                draftMap.TryGetValue(snapshot.Id, out var selectedOriginal);
                selectedOriginal = selectedOriginal?.Trim().ToUpperInvariant();

                snapshot.SelectedOriginalKey = selectedOriginal;
                snapshot.IsCorrect =
                    !string.IsNullOrWhiteSpace(selectedOriginal) &&
                    string.Equals(selectedOriginal, snapshot.CorrectOriginalKey, StringComparison.OrdinalIgnoreCase);

                if (snapshot.IsCorrect == true)
                    scoreValue++;
            }

            await _db.SaveChangesAsync(cancellationToken);

            var oldAnswers = await _db.Set<AttemptAnswer>()
                .Where(x => x.ExamAttemptId == attempt.Id)
                .ToListAsync(cancellationToken);

            if (oldAnswers.Count > 0)
                _db.Set<AttemptAnswer>().RemoveRange(oldAnswers);

            foreach (var snapshot in snapshots)
            {
                if (string.IsNullOrWhiteSpace(snapshot.SelectedOriginalKey))
                    continue;

                _db.Set<AttemptAnswer>().Add(new AttemptAnswer
                {
                    Id = Guid.NewGuid(),
                    ExamAttemptId = attempt.Id,
                    ExamQuestionId = snapshot.OriginalQuestionId,
                    SelectedAnswer = snapshot.SelectedOriginalKey,
                    CorrectAnswer = snapshot.CorrectOriginalKey ?? string.Empty,
                    IsCorrect = snapshot.IsCorrect ?? false,
                    Explanation = snapshot.Explanation
                });
            }

            var totalQuestionsValue = snapshots.Count;
            var percentageValue = totalQuestionsValue == 0
                ? 0
                : (int)Math.Round((decimal)scoreValue / totalQuestionsValue * 100m);

            var affected = await _db.Set<ExamAttempt>()
                .Where(x =>
                    x.Id == attempt.Id &&
                    x.Status == ExamAttemptStatus.Started &&
                    x.SubmittedAtUtc == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Score, scoreValue)
                    .SetProperty(x => x.TotalQuestions, totalQuestionsValue)
                    .SetProperty(x => x.Percentage, percentageValue)
                    .SetProperty(x => x.SubmittedAtUtc, DateTime.UtcNow)
                    .SetProperty(x => x.Status, ExamAttemptStatus.ClosedForViolation),
                    cancellationToken);

            if (affected == 0)
            {
                return Ok(new
                {
                    attemptClosed = true,
                    closedForViolation = true,
                    status = "ClosedForViolation",
                    message = "تم غلق الاختبار بالفعل."
                });
            }

            await _db.SaveChangesAsync(cancellationToken);

            score = scoreValue;
            totalQuestions = totalQuestionsValue;
            percentage = percentageValue;
        }

        return Ok(new
        {
            attemptId = attempt.Id,
            violationsCount,
            warningAt,
            closeAt,
            shouldWarn,
            closedForViolation = shouldClose,
            autoSubmitted = shouldClose,
            status = shouldClose ? "ClosedForViolation" : "Started",
            score,
            totalQuestions,
            percentage,
            message = shouldClose
                ? "تم غلق الاختبار بسبب تكرار المخالفات، وتم احتساب الإجابات التي تم حلها."
                : shouldWarn
                    ? "تم تسجيل مخالفات. في حال التكرار سيتم غلق الاختبار."
                    : "تم تسجيل المخالفة.",
            registeredAtUtc = DateTime.UtcNow
        });
    }

    [HttpGet("{examId:guid}/progress")]
    public async Task<IActionResult> GetProgress(
        Guid examId,
        CancellationToken cancellationToken)
    {
        var studentId = GetStudentProfileId();

        var attempt = await _db.Set<ExamAttempt>()
            .AsNoTracking()
            .Include(x => x.DraftAnswers)
            .Include(x => x.Violations)
            .FirstOrDefaultAsync(x =>
                x.ExamId == examId &&
                x.StudentProfileId == studentId &&
                x.Status == ExamAttemptStatus.Started &&
                x.SubmittedAtUtc == null,
                cancellationToken);

        if (attempt is null)
            return Ok(new
            {
                hasActiveAttempt = false,
                answers = Array.Empty<object>(),
                violationsCount = 0
            });

        return Ok(new
        {
            hasActiveAttempt = true,
            attemptId = attempt.Id,
            startedAtUtc = attempt.StartedAtUtc,
            answers = attempt.DraftAnswers
                .OrderBy(x => x.SavedAtUtc)
                .Select(x => new
                {
                    questionSnapshotId = x.QuestionSnapshotId,
                    selectedAnswer = x.SelectedAnswer,
                    savedAtUtc = x.SavedAtUtc
                })
                .ToList(),
            violationsCount = attempt.Violations.Count
        });
    }

    private async Task<int> UpsertDraftAnswersDirectAsync(
        Guid attemptId,
        HashSet<Guid> validSnapshotIds,
        List<SaveExamDraftAnswerItem> answers,
        CancellationToken cancellationToken)
    {
        var incoming = answers
            .Where(x => x.QuestionSnapshotId != Guid.Empty)
            .Where(x => validSnapshotIds.Contains(x.QuestionSnapshotId))
            .Where(x => !string.IsNullOrWhiteSpace(NormalizeAnswer(x.SelectedAnswer)))
            .GroupBy(x => x.QuestionSnapshotId)
            .Select(x => new
            {
                QuestionSnapshotId = x.Key,
                SelectedAnswer = NormalizeAnswer(x.Last().SelectedAnswer)!
            })
            .ToList();

        var now = DateTime.UtcNow;

        foreach (var item in incoming)
        {
            var existing = await _db.Set<ExamAttemptDraftAnswer>()
                .FirstOrDefaultAsync(x =>
                    x.ExamAttemptId == attemptId &&
                    x.QuestionSnapshotId == item.QuestionSnapshotId,
                    cancellationToken);

            if (existing is null)
            {
                _db.Set<ExamAttemptDraftAnswer>().Add(new ExamAttemptDraftAnswer
                {
                    Id = Guid.NewGuid(),
                    ExamAttemptId = attemptId,
                    QuestionSnapshotId = item.QuestionSnapshotId,
                    SelectedAnswer = item.SelectedAnswer,
                    SavedAtUtc = now
                });
            }
            else
            {
                existing.SelectedAnswer = item.SelectedAnswer;
                existing.SavedAtUtc = now;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return incoming.Count;
    }

    private Guid GetStudentProfileId()
    {
        var raw =
            User.FindFirstValue("studentProfileId") ??
            User.FindFirstValue("StudentProfileId") ??
            User.FindFirstValue("profileId") ??
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(raw, out var studentId))
            throw new UnauthorizedAccessException("Student profile id not found in token");

        return studentId;
    }

    private static string? NormalizeAnswer(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim().ToUpperInvariant();
        return normalized is "A" or "B" or "C" or "D" ? normalized : null;
    }
}

public class SaveExamDraftRequest
{
    public List<SaveExamDraftAnswerItem>? Answers { get; set; }
}

public class SaveExamDraftAnswerItem
{
    public Guid QuestionSnapshotId { get; set; }
    public string? SelectedAnswer { get; set; }
}

public class RegisterExamViolationRequest
{
    public string? Type { get; set; }
    public string? Details { get; set; }
    public List<SaveExamDraftAnswerItem>? Answers { get; set; }
}
