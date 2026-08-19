using Microsoft.EntityFrameworkCore;
using QuizSystem.Application.Contracts.Attempts;
using QuizSystem.Application.DTOs;
using QuizSystem.Domain.Entities;
using QuizSystem.Domain.Enums;
using QuizSystem.Infrastructure.Persistence;

namespace QuizSystem.Infrastructure.Services.Attempts;

public class StudentExamService : IStudentExamService
{
    private readonly AppDbContext _db;

    public StudentExamService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<object>> GetAvailableExamsForStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var exams = await _db.Registrations
            .AsNoTracking()
            .Where(x => x.StudentProfileId == studentId && x.IsActive)
            .Select(x => new
            {
                examId = x.Exam.Id,
                title = x.Exam.Title,
                examCode = x.Exam.ExamCode,
                startAtUtc = x.Exam.StartAtUtc,
                endAtUtc = x.Exam.EndAtUtc,
                isPublished = x.Exam.IsPublished,
                maxAttempts = x.Exam.MaxAttempts
            })
            .ToListAsync(cancellationToken);

        var closedAttempts = await _db.Set<ExamAttempt>()
            .AsNoTracking()
            .Where(x =>
                x.StudentProfileId == studentId &&
                (
                    x.Status == ExamAttemptStatus.Submitted ||
                    x.Status == ExamAttemptStatus.ClosedForViolation
                ) &&
                x.SubmittedAtUtc != null)
            .GroupBy(x => x.ExamId)
            .Select(x => new { ExamId = x.Key, Count = x.Count() })
            .ToListAsync(cancellationToken);

        var attemptCounts = closedAttempts.ToDictionary(x => x.ExamId, x => x.Count);

        return exams
            .Select(x => new
            {
                x.examId,
                x.title,
                x.examCode,
                x.startAtUtc,
                x.endAtUtc,
                x.isPublished,
                attemptsUsed = attemptCounts.GetValueOrDefault(x.examId),
                x.maxAttempts,
                isSubmitted = attemptCounts.GetValueOrDefault(x.examId) > 0,
                canStart =
                    x.isPublished &&
                    now >= x.startAtUtc &&
                    now <= x.endAtUtc &&
                    attemptCounts.GetValueOrDefault(x.examId) < x.maxAttempts,
                availabilityStatus =
                    !x.isPublished ? "غير منشور" :
                    attemptCounts.GetValueOrDefault(x.examId) >= x.maxAttempts ? "تم استنفاد عدد المحاولات" :
                    now < x.startAtUtc ? "لم يبدأ بعد" :
                    now > x.endAtUtc ? "انتهى الوقت" :
                    "متاح الآن"
            })
            .Cast<object>()
            .ToList();
    }

    public async Task<StartExamResponse> StartExamAsync(
        Guid studentId,
        Guid examId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var exam = await _db.Exams
            .AsNoTracking()
            .Include(x => x.Questions)
            .FirstOrDefaultAsync(x => x.Id == examId, cancellationToken)
            ?? throw new InvalidOperationException("Exam not found");

        if (!exam.IsPublished)
            throw new InvalidOperationException("Exam is not published yet");

        if (now < exam.StartAtUtc)
            throw new InvalidOperationException("Exam has not started yet");

        if (now > exam.EndAtUtc)
            throw new InvalidOperationException("Exam has already ended");

        var studentExists = await _db.Students
            .AsNoTracking()
            .AnyAsync(x => x.Id == studentId && x.IsActive, cancellationToken);

        if (!studentExists)
            throw new InvalidOperationException("Student not found");

        var isRegistered = await _db.Registrations
            .AsNoTracking()
            .AnyAsync(x =>
                x.StudentProfileId == studentId &&
                x.ExamId == examId &&
                x.IsActive,
                cancellationToken);

        if (!isRegistered)
            throw new InvalidOperationException("Student is not registered for this exam");

        var closedAttemptsCount = await _db.Set<ExamAttempt>()
            .AsNoTracking()
            .CountAsync(x =>
                x.StudentProfileId == studentId &&
                x.ExamId == examId &&
                x.SubmittedAtUtc != null &&
                (
                    x.Status == ExamAttemptStatus.Submitted ||
                    x.Status == ExamAttemptStatus.ClosedForViolation
                ),
                cancellationToken);

        if (closedAttemptsCount >= Math.Max(1, exam.MaxAttempts))
            throw new InvalidOperationException("لقد استنفدت العدد المسموح من محاولات هذا الاختبار");

        if (exam.Questions is null || exam.Questions.Count == 0)
            throw new InvalidOperationException("Exam has no questions");

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var attempt = await _db.Set<ExamAttempt>()
                .Include(x => x.QuestionSnapshots)
                .FirstOrDefaultAsync(x =>
                    x.StudentProfileId == studentId &&
                    x.ExamId == examId &&
                    x.Status == ExamAttemptStatus.Started &&
                    x.SubmittedAtUtc == null,
                    cancellationToken);

            if (attempt is not null && attempt.QuestionSnapshots.Count > 0)
            {
                await transaction.CommitAsync(cancellationToken);

                return new StartExamResponse
                {
                    AttemptId = attempt.Id,
                    ExamId = exam.Id,
                    Title = exam.Title,
                    ExamCode = exam.ExamCode,
                    StartAtUtc = exam.StartAtUtc,
                    EndAtUtc = exam.EndAtUtc,
                    AllowStudentExit = exam.AllowStudentExit,
                    EnableAntiCheat = exam.EnableAntiCheat,
                    MaxViolationCount = exam.MaxViolationCount <= 0 ? 3 : exam.MaxViolationCount,
                    Questions = BuildSnapshotQuestionViews(attempt.QuestionSnapshots.OrderBy(x => x.DisplayOrder).ToList())
                };
            }

            // حذف أي محاولة Started تالفة بدون Snapshot
            if (attempt is not null)
            {
                _db.Set<ExamAttempt>().Remove(attempt);
                await _db.SaveChangesAsync(cancellationToken);
            }

            attempt = new ExamAttempt
            {
                Id = Guid.NewGuid(),
                InstitutionId = exam.InstitutionId,
                ExamId = exam.Id,
                StudentProfileId = studentId,
                AttemptNumber = closedAttemptsCount + 1,
                StartedAtUtc = now,
                SubmittedAtUtc = null,
                Status = ExamAttemptStatus.Started,
                Score = 0,
                TotalQuestions = 0,
                Percentage = 0
            };

            _db.Set<ExamAttempt>().Add(attempt);

            var selectedQuestions = exam.Questions
                .OrderBy(x => x.CreatedAtUtc)
                .Take(Math.Min(
                    exam.ExamQuestionCount > 0 ? exam.ExamQuestionCount : exam.Questions.Count,
                    exam.Questions.Count))
                .OrderBy(x => Guid.NewGuid())
                .ToList();

            var displayOrder = 1;

            foreach (var q in selectedQuestions)
            {
                var choices = new List<(string Key, string Text, string? Image)>
                {
                    ("A", q.ChoiceA, q.ChoiceAImageUrl),
                    ("B", q.ChoiceB, q.ChoiceBImageUrl),
                    ("C", q.ChoiceC, q.ChoiceCImageUrl),
                    ("D", q.ChoiceD, q.ChoiceDImageUrl)
                }
                .OrderBy(x => Guid.NewGuid())
                .ToList();

                attempt.QuestionSnapshots.Add(new ExamAttemptQuestionSnapshot
                {
                    Id = Guid.NewGuid(),
                    InstitutionId = exam.InstitutionId,
                    ExamAttemptId = attempt.Id,
                    OriginalQuestionId = q.Id,
                    DisplayOrder = displayOrder++,

                    QuestionText = q.QuestionText,
                    QuestionImageUrl = q.QuestionImageUrl,

                    ChoiceADisplayLabel = "A",
                    ChoiceAOriginalKey = choices[0].Key,
                    ChoiceAText = choices[0].Text,
                    ChoiceAImageUrl = choices[0].Image,

                    ChoiceBDisplayLabel = "B",
                    ChoiceBOriginalKey = choices[1].Key,
                    ChoiceBText = choices[1].Text,
                    ChoiceBImageUrl = choices[1].Image,

                    ChoiceCDisplayLabel = "C",
                    ChoiceCOriginalKey = choices[2].Key,
                    ChoiceCText = choices[2].Text,
                    ChoiceCImageUrl = choices[2].Image,

                    ChoiceDDisplayLabel = "D",
                    ChoiceDOriginalKey = choices[3].Key,
                    ChoiceDText = choices[3].Text,
                    ChoiceDImageUrl = choices[3].Image,

                    CorrectOriginalKey = q.CorrectAnswer?.Trim().ToUpperInvariant(),
                    Explanation = q.Explanation
                });
            }

            attempt.TotalQuestions = attempt.QuestionSnapshots.Count;

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new StartExamResponse
            {
                AttemptId = attempt.Id,
                ExamId = exam.Id,
                Title = exam.Title,
                ExamCode = exam.ExamCode,
                StartAtUtc = exam.StartAtUtc,
                EndAtUtc = exam.EndAtUtc,
                AllowStudentExit = exam.AllowStudentExit,
                EnableAntiCheat = exam.EnableAntiCheat,
                MaxViolationCount = exam.MaxViolationCount <= 0 ? 3 : exam.MaxViolationCount,
                Questions = BuildSnapshotQuestionViews(attempt.QuestionSnapshots.OrderBy(x => x.DisplayOrder).ToList())
            };
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new InvalidOperationException("حدث تعارض أثناء بدء الاختبار. أعد المحاولة بعد تحديث الصفحة.", ex);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ExamResultResponse> SubmitExamAsync(
        Guid studentId,
        SubmitExamRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new InvalidOperationException("Request is required");

        if (request.ExamId == Guid.Empty)
            throw new InvalidOperationException("ExamId is required");

        request.Answers ??= new List<SubmitAnswerItem>();

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var attemptInfo = await _db.Set<ExamAttempt>()
                .AsNoTracking()
                .Where(x =>
                    x.StudentProfileId == studentId &&
                    x.ExamId == request.ExamId &&
                    x.Status == ExamAttemptStatus.Started &&
                    x.SubmittedAtUtc == null)
                .Select(x => new
                {
                    x.Id,
                    x.ExamId,
                    x.InstitutionId
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (attemptInfo is null)
                throw new InvalidOperationException("Attempt not found. Start the exam first.");

            var snapshots = await _db.Set<ExamAttemptQuestionSnapshot>()
                .Where(x => x.ExamAttemptId == attemptInfo.Id)
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync(cancellationToken);

            if (snapshots.Count == 0)
                throw new InvalidOperationException("Attempt snapshot was not found");

            var submittedMap = request.Answers
                .Where(x =>
                    (x.QuestionSnapshotId != Guid.Empty || x.QuestionId != Guid.Empty) &&
                    !string.IsNullOrWhiteSpace(x.SelectedAnswer))
                .GroupBy(x => x.QuestionSnapshotId != Guid.Empty ? x.QuestionSnapshotId : x.QuestionId)
                .ToDictionary(
                    x => x.Key,
                    x => x.Last().SelectedAnswer!.Trim().ToUpperInvariant());

            var validSnapshotIds = snapshots.Select(x => x.Id).ToHashSet();

            submittedMap = submittedMap
                .Where(x => validSnapshotIds.Contains(x.Key))
                .ToDictionary(x => x.Key, x => x.Value);

            // خزّن payload في DraftAnswers كـ upsert مباشر
            foreach (var pair in submittedMap)
            {
                var existing = await _db.Set<ExamAttemptDraftAnswer>()
                    .FirstOrDefaultAsync(x =>
                        x.ExamAttemptId == attemptInfo.Id &&
                        x.QuestionSnapshotId == pair.Key,
                        cancellationToken);

                if (existing is null)
                {
                    _db.Set<ExamAttemptDraftAnswer>().Add(new ExamAttemptDraftAnswer
                    {
                        Id = Guid.NewGuid(),
                        InstitutionId = attemptInfo.InstitutionId,
                        ExamAttemptId = attemptInfo.Id,
                        QuestionSnapshotId = pair.Key,
                        SelectedAnswer = pair.Value,
                        SavedAtUtc = DateTime.UtcNow
                    });
                }
                else
                {
                    existing.SelectedAnswer = pair.Value;
                    existing.SavedAtUtc = DateTime.UtcNow;
                }
            }

            if (submittedMap.Count > 0)
                await _db.SaveChangesAsync(cancellationToken);

            var draftMap = await _db.Set<ExamAttemptDraftAnswer>()
                .AsNoTracking()
                .Where(x =>
                    x.ExamAttemptId == attemptInfo.Id &&
                    !string.IsNullOrWhiteSpace(x.SelectedAnswer))
                .GroupBy(x => x.QuestionSnapshotId)
                .Select(g => new
                {
                    QuestionSnapshotId = g.Key,
                    SelectedAnswer = g.OrderByDescending(x => x.SavedAtUtc).First().SelectedAnswer!
                })
                .ToDictionaryAsync(
                    x => x.QuestionSnapshotId,
                    x => x.SelectedAnswer.Trim().ToUpperInvariant(),
                    cancellationToken);

            var finalAnswers = submittedMap.Count > 0 ? submittedMap : draftMap;

            var score = 0;

            foreach (var snapshot in snapshots)
            {
                finalAnswers.TryGetValue(snapshot.Id, out var selectedOriginal);
                selectedOriginal = selectedOriginal?.Trim().ToUpperInvariant();

                snapshot.SelectedOriginalKey = selectedOriginal;
                snapshot.IsCorrect =
                    !string.IsNullOrWhiteSpace(selectedOriginal) &&
                    string.Equals(selectedOriginal, snapshot.CorrectOriginalKey, StringComparison.OrdinalIgnoreCase);

                if (snapshot.IsCorrect == true)
                    score++;
            }

            await _db.SaveChangesAsync(cancellationToken);

            // احذف الإجابات القديمة ثم أعد بناء AttemptAnswers
            var oldAnswers = await _db.Set<AttemptAnswer>()
                .Where(x => x.ExamAttemptId == attemptInfo.Id)
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
                    InstitutionId = attemptInfo.InstitutionId,
                    ExamAttemptId = attemptInfo.Id,
                    ExamQuestionId = snapshot.OriginalQuestionId,
                    SelectedAnswer = snapshot.SelectedOriginalKey,
                    CorrectAnswer = snapshot.CorrectOriginalKey ?? string.Empty,
                    IsCorrect = snapshot.IsCorrect ?? false,
                    Explanation = snapshot.Explanation
                });
            }

            var totalQuestions = request.IsAutoSubmitDueToExit
                ? finalAnswers.Count
                : snapshots.Count;

            var percentage = totalQuestions == 0
                ? 0
                : (int)Math.Round((decimal)score / totalQuestions * 100m);

            // تحديث ExamAttempt مباشرة بدون تحميله لتجنب Concurrency
            var affected = await _db.Set<ExamAttempt>()
                .Where(x =>
                    x.Id == attemptInfo.Id &&
                    x.Status == ExamAttemptStatus.Started &&
                    x.SubmittedAtUtc == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Score, score)
                    .SetProperty(x => x.TotalQuestions, totalQuestions)
                    .SetProperty(x => x.Percentage, percentage)
                    .SetProperty(x => x.SubmittedAtUtc, DateTime.UtcNow)
                    .SetProperty(x => x.Status, ExamAttemptStatus.Submitted),
                    cancellationToken);

            if (affected == 0)
                throw new InvalidOperationException("Attempt was already submitted or closed.");

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new ExamResultResponse
            {
                AttemptId = attemptInfo.Id,
                ExamId = attemptInfo.ExamId,
                Score = score,
                TotalQuestions = totalQuestions,
                Percentage = percentage
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<List<ParentChildResultResponse>> GetChildrenResultsAsync(
        Guid parentProfileId,
        CancellationToken cancellationToken = default)
    {
        var linkedStudents = await _db.ParentStudentLinks
            .AsNoTracking()
            .Where(x => x.ParentProfileId == parentProfileId)
            .Select(x => new
            {
                x.StudentProfileId,
                StudentName = x.StudentProfile.FullName,
                StudentCode = x.StudentProfile.StudentCode
            })
            .ToListAsync(cancellationToken);

        var studentIds = linkedStudents.Select(x => x.StudentProfileId).Distinct().ToList();

        if (studentIds.Count == 0)
            return new List<ParentChildResultResponse>();

        var registrations = await _db.Set<ExamRegistration>()
            .AsNoTracking()
            .Where(r => studentIds.Contains(r.StudentProfileId) && r.IsActive)
            .Include(r => r.Exam)
            .ToListAsync(cancellationToken);

        var attempts = await _db.Set<ExamAttempt>()
            .AsNoTracking()
            .Where(a => studentIds.Contains(a.StudentProfileId))
            .ToListAsync(cancellationToken);

        var attemptsLookup = attempts
            .GroupBy(a => new { a.StudentProfileId, a.ExamId })
            .ToDictionary(
                g => (g.Key.StudentProfileId, g.Key.ExamId),
                g => g.OrderByDescending(x => x.SubmittedAtUtc ?? x.StartedAtUtc).First());

        var studentsLookup = linkedStudents.ToDictionary(
            x => x.StudentProfileId,
            x => new { x.StudentName, x.StudentCode });

        return registrations
            .Select(r =>
            {
                attemptsLookup.TryGetValue((r.StudentProfileId, r.ExamId), out var attempt);
                var student = studentsLookup[r.StudentProfileId];

                return new ParentChildResultResponse
                {
                    StudentId = r.StudentProfileId,
                    StudentName = student.StudentName,
                    StudentCode = student.StudentCode,
                    ExamTitle = r.Exam?.Title ?? string.Empty,
                    ExamCode = r.Exam?.ExamCode ?? string.Empty,
                    Score = attempt?.Score ?? 0,
                    TotalQuestions = attempt?.TotalQuestions ?? 0,
                    Percentage = attempt?.Percentage ?? 0,
                    SubmittedAtUtc = attempt?.SubmittedAtUtc,
                    IsSubmitted = attempt is not null && attempt.SubmittedAtUtc.HasValue
                };
            })
            .OrderBy(x => x.StudentName)
            .ThenBy(x => x.ExamTitle)
            .ToList();
    }

    private static void UpsertDraftAnswers(ExamAttempt attempt, Dictionary<Guid, string> answers)
    {
        var validSnapshotIds = attempt.QuestionSnapshots.Select(x => x.Id).ToHashSet();

        foreach (var pair in answers)
        {
            if (!validSnapshotIds.Contains(pair.Key))
                continue;

            var existing = attempt.DraftAnswers.FirstOrDefault(x => x.QuestionSnapshotId == pair.Key);

            if (existing is null)
            {
                attempt.DraftAnswers.Add(new ExamAttemptDraftAnswer
                {
                    Id = Guid.NewGuid(),
                    InstitutionId = attempt.InstitutionId,
                    ExamAttemptId = attempt.Id,
                    QuestionSnapshotId = pair.Key,
                    SelectedAnswer = pair.Value,
                    SavedAtUtc = DateTime.UtcNow
                });
            }
            else
            {
                existing.SelectedAnswer = pair.Value;
                existing.SavedAtUtc = DateTime.UtcNow;
            }
        }
    }

    private static void ApplyAnswersToAttempt(
        ExamAttempt attempt,
        Dictionary<Guid, string> answers,
        bool countAnsweredOnly,
        ExamAttemptStatus finalStatus)
    {
        var snapshots = attempt.QuestionSnapshots.OrderBy(x => x.DisplayOrder).ToList();

        attempt.Answers.Clear();

        var score = 0;

        foreach (var snapshot in snapshots)
        {
            answers.TryGetValue(snapshot.Id, out var selectedOriginal);

            selectedOriginal = selectedOriginal?.Trim().ToUpperInvariant();

            snapshot.SelectedOriginalKey = selectedOriginal;
            snapshot.IsCorrect =
                !string.IsNullOrWhiteSpace(selectedOriginal) &&
                string.Equals(selectedOriginal, snapshot.CorrectOriginalKey, StringComparison.OrdinalIgnoreCase);

            if (snapshot.IsCorrect == true)
                score++;

            if (!string.IsNullOrWhiteSpace(selectedOriginal))
            {
                attempt.Answers.Add(new AttemptAnswer
                {
                    Id = Guid.NewGuid(),
                    InstitutionId = attempt.InstitutionId,
                    ExamAttemptId = attempt.Id,
                    ExamQuestionId = snapshot.OriginalQuestionId,
                    SelectedAnswer = selectedOriginal,
                    CorrectAnswer = snapshot.CorrectOriginalKey ?? string.Empty,
                    IsCorrect = snapshot.IsCorrect ?? false,
                    Explanation = snapshot.Explanation
                });
            }
        }

        var total = countAnsweredOnly
            ? answers.Count(x => !string.IsNullOrWhiteSpace(x.Value))
            : snapshots.Count;

        attempt.Score = score;
        attempt.TotalQuestions = total;
        attempt.Percentage = total == 0
            ? 0
            : (int)Math.Round((decimal)score / total * 100m);

        attempt.SubmittedAtUtc = DateTime.UtcNow;
        attempt.Status = finalStatus;
    }

    private static List<ExamQuestionView> BuildSnapshotQuestionViews(List<ExamAttemptQuestionSnapshot> snapshots)
    {
        return snapshots
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new ExamQuestionView
            {
                Id = x.Id,
                QuestionText = x.QuestionText,
                QuestionImageUrl = x.QuestionImageUrl,
                Choices = new List<ExamChoiceView>
                {
                    new ExamChoiceView { DisplayLabel = x.ChoiceADisplayLabel, OriginalKey = x.ChoiceAOriginalKey, Text = x.ChoiceAText },
                    new ExamChoiceView { DisplayLabel = x.ChoiceBDisplayLabel, OriginalKey = x.ChoiceBOriginalKey, Text = x.ChoiceBText },
                    new ExamChoiceView { DisplayLabel = x.ChoiceCDisplayLabel, OriginalKey = x.ChoiceCOriginalKey, Text = x.ChoiceCText },
                    new ExamChoiceView { DisplayLabel = x.ChoiceDDisplayLabel, OriginalKey = x.ChoiceDOriginalKey, Text = x.ChoiceDText }
                }
            })
            .ToList();
    }
}
