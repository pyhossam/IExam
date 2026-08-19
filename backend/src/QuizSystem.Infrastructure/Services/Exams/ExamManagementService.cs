using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
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
using System.Text.Json;

namespace QuizSystem.Infrastructure.Services.Exams;
public class ExamManagementService : IExamManagementService
{
    private readonly AppDbContext _db;
    private readonly IAiQuestionGenerator _aiQuestionGenerator;

    public ExamManagementService(AppDbContext db, IAiQuestionGenerator aiQuestionGenerator)
    {
        _db = db;
        _aiQuestionGenerator = aiQuestionGenerator;
    }

    public async Task<Guid> CreateAiExamAsync(Guid institutionId, Guid createdByUserId, CreateAiExamRequest request, CancellationToken cancellationToken = default)
    {
        ValidateExamWindow(request.StartAtUtc, request.EndAtUtc);
        ValidateCounts(request.BankQuestionCount, request.ExamQuestionCount);

        if (await _db.Exams.AnyAsync(x => x.InstitutionId == institutionId && x.ExamCode == request.ExamCode, cancellationToken))
            throw new InvalidOperationException("Exam code already exists");

        var exam = new Exam
        {
            SubjectId = request.SubjectId,
            AssessmentType = ParseAssessmentType(request.AssessmentType),
            MaxAttempts = ValidateMaxAttempts(request.MaxAttempts),
            InstitutionId = institutionId,
            Title = request.Title,
            Topic = request.Topic,
            Description = request.Description,
            ExamCode = request.ExamCode,
            StartAtUtc = request.StartAtUtc,
            EndAtUtc = request.EndAtUtc,
            BankQuestionCount = request.BankQuestionCount,
            ExamQuestionCount = request.ExamQuestionCount,
            CreatedManually = false,
            CreatedByUserId = createdByUserId,
            IsPublished = true
        };

        _db.Exams.Add(exam);
        await _db.SaveChangesAsync(cancellationToken);

        var generatedQuestions = await _aiQuestionGenerator.GenerateQuestionsAsync(
            request.Topic,
            request.BankQuestionCount,
            null,
            null,
            cancellationToken
        );

        foreach (var q in generatedQuestions)
        {
            _db.Questions.Add(new ExamQuestion
            {
                InstitutionId = institutionId,
                ExamId = exam.Id,
                QuestionText = q.QuestionText,
                QuestionImageUrl = q.QuestionImageUrl,
                ChoiceA = q.ChoiceA,
                ChoiceAImageUrl = q.ChoiceAImageUrl,
                ChoiceB = q.ChoiceB,
                ChoiceBImageUrl = q.ChoiceBImageUrl,
                ChoiceC = q.ChoiceC,
                ChoiceCImageUrl = q.ChoiceCImageUrl,
                ChoiceD = q.ChoiceD,
                ChoiceDImageUrl = q.ChoiceDImageUrl,
                CorrectAnswer = NormalizeAnswer(q.CorrectAnswer),
                Explanation = q.Explanation
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return exam.Id;
    }

    public async Task<Guid> CreateManualExamAsync(Guid institutionId, Guid createdByUserId, CreateManualExamRequest request, CancellationToken cancellationToken = default)
    {
        ValidateExamWindow(request.StartAtUtc, request.EndAtUtc);

        if (await _db.Exams.AnyAsync(x => x.InstitutionId == institutionId && x.ExamCode == request.ExamCode, cancellationToken))
            throw new InvalidOperationException("Exam code already exists");

        var exam = new Exam
        {
            SubjectId = request.SubjectId,
            AssessmentType = ParseAssessmentType(request.AssessmentType),
            MaxAttempts = ValidateMaxAttempts(request.MaxAttempts),
            InstitutionId = institutionId,
            Title = request.Title,
            Topic = request.Topic,
            Description = request.Description,
            ExamCode = request.ExamCode,
            StartAtUtc = request.StartAtUtc,
            EndAtUtc = request.EndAtUtc,
            BankQuestionCount = 0,
            ExamQuestionCount = request.ExamQuestionCount,
            CreatedManually = true,
            CreatedByUserId = createdByUserId,
            IsPublished = true
        };

        _db.Exams.Add(exam);
        await _db.SaveChangesAsync(cancellationToken);

        return exam.Id;
    }

    public async Task<List<ExamListItemDto>> GetExamsAsync(Guid? institutionId, bool isSuperAdmin, CancellationToken cancellationToken = default)
    {
        var query = _db.Exams.AsQueryable();

        if (!isSuperAdmin)
        {
            var tenantId = RequireTenantInstitution(institutionId);
            query = query.Where(x => x.InstitutionId == tenantId);
        }

        return await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new ExamListItemDto
            {
                Id = x.Id,
                SubjectId = x.SubjectId,
                AssessmentType = x.AssessmentType.ToString(),
                MaxAttempts = x.MaxAttempts,
                Title = x.Title,
                Topic = x.Topic,
                ExamCode = x.ExamCode,
                BankQuestionCount = x.BankQuestionCount,
                ExamQuestionCount = x.ExamQuestionCount,
                StartAtUtc = x.StartAtUtc,
                EndAtUtc = x.EndAtUtc,
                CreatedManually = x.CreatedManually,
                IsPublished = x.IsPublished,
                AllowStudentExit = x.AllowStudentExit,
                EnableAntiCheat = x.EnableAntiCheat,
                MaxViolationCount = x.MaxViolationCount,
                InstitutionId = _db.Users
                    .Where(u => u.Id == x.CreatedByUserId)
                    .Select(u => u.InstitutionId)
                    .FirstOrDefault(),
                InstitutionName = _db.Users
                    .Where(u => u.Id == x.CreatedByUserId)
                    .Select(u => u.Institution != null ? u.Institution.Name : null)
                    .FirstOrDefault(),
                RegisteredStudentsCount = x.Registrations.Count,
                AttemptsCount = x.Attempts.Count
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ExamDetailsDto> GetExamDetailsAsync(Guid? institutionId, bool isSuperAdmin, Guid examId, CancellationToken cancellationToken = default)
    {
        var query = _db.Exams.AsQueryable();

        if (!isSuperAdmin)
        {
            var tenantId = RequireTenantInstitution(institutionId);
            query = query.Where(x => x.InstitutionId == tenantId);
        }

        var exam = await query
            .Include(x => x.Questions)
            .ThenInclude(x => x.CourseLearningOutcome)
            .Include(x => x.Subject)
            .FirstOrDefaultAsync(x => x.Id == examId, cancellationToken)
            ?? throw new InvalidOperationException("Exam not found");

        var institution = await _db.Users
            .Where(x => x.Id == exam.CreatedByUserId)
            .Select(x => new
            {
                x.InstitutionId,
                InstitutionName = x.Institution != null ? x.Institution.Name : null
            })
            .FirstOrDefaultAsync(cancellationToken);

        return new ExamDetailsDto
        {
            Id = exam.Id,
            SubjectId = exam.SubjectId,
            SubjectName = exam.Subject?.Name,
            SubjectCode = exam.Subject?.Code,
            AssessmentType = exam.AssessmentType.ToString(),
            MaxAttempts = exam.MaxAttempts,
            Title = exam.Title,
            Topic = exam.Topic,
            Description = exam.Description,
            ExamCode = exam.ExamCode,
            BankQuestionCount = exam.BankQuestionCount,
            ExamQuestionCount = exam.ExamQuestionCount,
            BlueprintCloDistribution = DeserializeDistribution(exam.BlueprintCloDistributionJson),
            BlueprintBloomDistribution = DeserializeDistribution(exam.BlueprintBloomDistributionJson),
            StartAtUtc = exam.StartAtUtc,
            EndAtUtc = exam.EndAtUtc,
            CreatedManually = exam.CreatedManually,
            IsPublished = exam.IsPublished,
                AllowStudentExit = exam.AllowStudentExit,
            EnableAntiCheat = exam.EnableAntiCheat,
            MaxViolationCount = exam.MaxViolationCount,
            InstitutionId = institution?.InstitutionId,
            InstitutionName = institution?.InstitutionName,
            Questions = exam.Questions
                .OrderBy(x => x.CreatedAtUtc)
                .Select(q => new ExamQuestionDto
                {
                    Id = q.Id,
                    CourseLearningOutcomeId = q.CourseLearningOutcomeId,
                    CloCode = q.CourseLearningOutcome == null ? null : q.CourseLearningOutcome.Code,
                    CognitiveLevel = q.CognitiveLevel.ToString(),
                    QuestionText = q.QuestionText,
                QuestionImageUrl = q.QuestionImageUrl,
                    ChoiceA = q.ChoiceA,
                ChoiceAImageUrl = q.ChoiceAImageUrl,
                    ChoiceB = q.ChoiceB,
                ChoiceBImageUrl = q.ChoiceBImageUrl,
                    ChoiceC = q.ChoiceC,
                ChoiceCImageUrl = q.ChoiceCImageUrl,
                    ChoiceD = q.ChoiceD,
                ChoiceDImageUrl = q.ChoiceDImageUrl,
                    CorrectAnswer = q.CorrectAnswer,
                    Explanation = q.Explanation
                })
                .ToList()
        };
    }

    public async Task UpdateExamSettingsAsync(Guid examId, UpdateExamSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var exam = await _db.Exams
            .Include(x => x.Questions)
            .FirstOrDefaultAsync(x => x.Id == examId, cancellationToken)
            ?? throw new InvalidOperationException("Exam not found");

        ValidateExamWindow(request.StartAtUtc, request.EndAtUtc);
        ValidateCounts(request.BankQuestionCount, request.ExamQuestionCount);

        if (request.BankQuestionCount < exam.Questions.Count)
            throw new InvalidOperationException("Bank question count cannot be less than the actual saved questions count");

        ValidateBlueprint(request.ExamQuestionCount, request.AssessmentType, request.BlueprintCloDistribution, request.BlueprintBloomDistribution);

        exam.Title = request.Title;
        exam.SubjectId = request.SubjectId;
        exam.AssessmentType = ParseAssessmentType(request.AssessmentType);
        exam.MaxAttempts = ValidateMaxAttempts(request.MaxAttempts);
        exam.Topic = request.Topic;
        exam.Description = request.Description;
        exam.StartAtUtc = request.StartAtUtc;
        exam.EndAtUtc = request.EndAtUtc;
        exam.BankQuestionCount = request.BankQuestionCount;
        exam.ExamQuestionCount = request.ExamQuestionCount;
        exam.BlueprintCloDistributionJson = JsonSerializer.Serialize(request.BlueprintCloDistribution ?? new());
        exam.BlueprintBloomDistributionJson = JsonSerializer.Serialize(request.BlueprintBloomDistribution ?? new());
        exam.IsPublished = request.IsPublished;
        exam.AllowStudentExit = request.AllowStudentExit;
        exam.EnableAntiCheat = request.EnableAntiCheat;
        exam.MaxViolationCount = request.MaxViolationCount <= 0 ? 3 : request.MaxViolationCount;
        exam.AllowStudentExit = request.AllowStudentExit;
        exam.EnableAntiCheat = request.EnableAntiCheat;
        exam.MaxViolationCount = request.MaxViolationCount <= 0 ? 3 : request.MaxViolationCount;

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static Dictionary<string, int> DeserializeDistribution(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try { return JsonSerializer.Deserialize<Dictionary<string, int>>(json) ?? new(); }
        catch (JsonException) { return new(); }
    }

    private static void ValidateBlueprint(int total, string assessmentType, Dictionary<string, int>? clo, Dictionary<string, int>? bloom)
    {
        clo ??= new();
        bloom ??= new();
        if (clo.Values.Any(x => x < 0) || bloom.Values.Any(x => x < 0))
            throw new InvalidOperationException("عدد الأسئلة في مخطط الورقة لا يمكن أن يكون سالبًا");
        if (clo.Values.Sum() > total)
            throw new InvalidOperationException("مجموع أسئلة CLO لا يمكن أن يتجاوز إجمالي أسئلة الورقة");
        if (bloom.Values.Sum() > total)
            throw new InvalidOperationException("مجموع أسئلة Bloom لا يمكن أن يتجاوز إجمالي أسئلة الورقة");
        if (string.Equals(assessmentType, "CloAligned", StringComparison.OrdinalIgnoreCase) && clo.ContainsKey("none") && clo["none"] > 0)
            throw new InvalidOperationException("الاختبار المرتبط بـ CLO لا يسمح بتخصيص أسئلة بدون CLO");
    }

    public async Task<Guid> AddQuestionAsync(Guid examId, UpsertExamQuestionRequest request, CancellationToken cancellationToken = default)
    {
        var exam = await _db.Exams.FirstOrDefaultAsync(x => x.Id == examId, cancellationToken)
            ?? throw new InvalidOperationException("Exam not found");
        ValidateQuestion(request);

        var question = new ExamQuestion
        {
            CourseLearningOutcomeId = request.CourseLearningOutcomeId,
            CognitiveLevel = ParseCognitiveLevel(request.CognitiveLevel),
            InstitutionId = exam.InstitutionId,
            ExamId = examId,
            QuestionText = request.QuestionText ?? string.Empty,
            QuestionImageUrl = request.QuestionImageUrl,
            ChoiceA = request.ChoiceA ?? string.Empty,
            ChoiceAImageUrl = request.ChoiceAImageUrl,
            ChoiceB = request.ChoiceB ?? string.Empty,
            ChoiceBImageUrl = request.ChoiceBImageUrl,
            ChoiceC = request.ChoiceC ?? string.Empty,
            ChoiceCImageUrl = request.ChoiceCImageUrl,
            ChoiceD = request.ChoiceD ?? string.Empty,
            ChoiceDImageUrl = request.ChoiceDImageUrl,
            CorrectAnswer = NormalizeAnswer(request.CorrectAnswer),
            Explanation = request.Explanation
        };

        _db.Questions.Add(question);
        exam.BankQuestionCount += 1;

        await _db.SaveChangesAsync(cancellationToken);
        return question.Id;
    }

    public async Task UpdateQuestionAsync(Guid questionId, UpsertExamQuestionRequest request, CancellationToken cancellationToken = default)
    {
        var question = await _db.Questions.FirstOrDefaultAsync(x => x.Id == questionId, cancellationToken)
            ?? throw new InvalidOperationException("Question not found");

        ValidateQuestion(request);

        question.QuestionText = request.QuestionText ?? string.Empty;
        question.CourseLearningOutcomeId = request.CourseLearningOutcomeId;
        question.CognitiveLevel = ParseCognitiveLevel(request.CognitiveLevel);
        question.QuestionImageUrl = request.QuestionImageUrl;
        question.ChoiceA = request.ChoiceA ?? string.Empty;
        question.ChoiceAImageUrl = request.ChoiceAImageUrl;
        question.ChoiceB = request.ChoiceB ?? string.Empty;
        question.ChoiceBImageUrl = request.ChoiceBImageUrl;
        question.ChoiceC = request.ChoiceC ?? string.Empty;
        question.ChoiceCImageUrl = request.ChoiceCImageUrl;
        question.ChoiceD = request.ChoiceD ?? string.Empty;
        question.ChoiceDImageUrl = request.ChoiceDImageUrl;
        question.CorrectAnswer = NormalizeAnswer(request.CorrectAnswer);
        question.Explanation = request.Explanation;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteQuestionAsync(Guid questionId, CancellationToken cancellationToken = default)
    {
        var question = await _db.Questions.FirstOrDefaultAsync(x => x.Id == questionId, cancellationToken)
            ?? throw new InvalidOperationException("Question not found");

        var exam = await _db.Exams.FirstAsync(x => x.Id == question.ExamId, cancellationToken);

        _db.Questions.Remove(question);
        exam.BankQuestionCount = Math.Max(0, exam.BankQuestionCount - 1);

        if (exam.ExamQuestionCount > exam.BankQuestionCount)
            exam.ExamQuestionCount = exam.BankQuestionCount;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<UploadQuestionsResultDto> UploadQuestionsAsync(Guid examId, IFormFile file, CancellationToken cancellationToken = default)
    {
        var exam = await _db.Exams.FirstOrDefaultAsync(x => x.Id == examId, cancellationToken)
            ?? throw new InvalidOperationException("Exam not found");
        var courseClos = await _db.CourseLearningOutcomes
            .Where(x => x.SubjectId == exam.SubjectId && x.InstitutionId == exam.InstitutionId && x.IsActive)
            .ToDictionaryAsync(x => x.Code.Trim(), x => x.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var result = new UploadQuestionsResultDto();

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (extension == ".xlsx")
        {
            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheet(1);

            var rows = sheet.RangeUsed()?.RowsUsed().ToList() ?? new List<IXLRangeRow>();
            if (rows.Count <= 1) return result;

            var headers = rows[0].Cells().Select(c => c.GetString().Trim().ToLowerInvariant()).ToList();
            var map = BuildHeaderMap(headers);

            foreach (var row in rows.Skip(1))
            {
                try
                {
                    var request = ReadQuestionFromExcelRow(row, map);
                    ApplyQuestionClassification(
                        request,
                        ReadOptionalExcelValue(row, map, "clo_code"),
                        ReadOptionalExcelValue(row, map, "cognitive_level"),
                        exam.AssessmentType,
                        courseClos);
                    ValidateQuestion(request);

                    _db.Questions.Add(new ExamQuestion
                    {
                        InstitutionId = exam.InstitutionId,
                        ExamId = examId,
                        CourseLearningOutcomeId = request.CourseLearningOutcomeId,
                        CognitiveLevel = ParseCognitiveLevel(request.CognitiveLevel),
                        QuestionText = request.QuestionText ?? string.Empty,
            QuestionImageUrl = request.QuestionImageUrl,
                        ChoiceA = request.ChoiceA ?? string.Empty,
            ChoiceAImageUrl = request.ChoiceAImageUrl,
                        ChoiceB = request.ChoiceB ?? string.Empty,
            ChoiceBImageUrl = request.ChoiceBImageUrl,
                        ChoiceC = request.ChoiceC ?? string.Empty,
            ChoiceCImageUrl = request.ChoiceCImageUrl,
                        ChoiceD = request.ChoiceD ?? string.Empty,
            ChoiceDImageUrl = request.ChoiceDImageUrl,
                        CorrectAnswer = NormalizeAnswer(request.CorrectAnswer),
                        Explanation = request.Explanation
                    });

                    result.Inserted++;
                }
                catch (Exception ex)
                {
                    result.Skipped++;
                    result.Errors.Add(ex.Message);
                }
            }
        }
        else if (extension == ".csv")
        {
            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream);
            var allLines = new List<string>();

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (line is not null)
                    allLines.Add(line);
            }

            if (allLines.Count <= 1) return result;

            var headers = allLines[0].Split(',').Select(x => x.Trim().ToLowerInvariant()).ToList();
            var map = BuildHeaderMap(headers);

            foreach (var line in allLines.Skip(1))
            {
                try
                {
                    var cols = line.Split(',');
                    var request = new UpsertExamQuestionRequest
                    {
                        QuestionText = GetOptionalValue(cols, map, "question_text") ?? string.Empty,
                        QuestionImageUrl = GetOptionalValue(cols, map, "question_image_url"),
                        ChoiceA = GetOptionalValue(cols, map, "choice_a") ?? string.Empty,
                        ChoiceAImageUrl = GetOptionalValue(cols, map, "choice_a_image_url"),
                        ChoiceB = GetOptionalValue(cols, map, "choice_b") ?? string.Empty,
                        ChoiceBImageUrl = GetOptionalValue(cols, map, "choice_b_image_url"),
                        ChoiceC = GetOptionalValue(cols, map, "choice_c") ?? string.Empty,
                        ChoiceCImageUrl = GetOptionalValue(cols, map, "choice_c_image_url"),
                        ChoiceD = GetOptionalValue(cols, map, "choice_d") ?? string.Empty,
                        ChoiceDImageUrl = GetOptionalValue(cols, map, "choice_d_image_url"),
                        CorrectAnswer = GetValue(cols, map, "correct_answer"),
                        Explanation = GetOptionalValue(cols, map, "explanation"),
                    };
                    ApplyQuestionClassification(
                        request,
                        GetOptionalValue(cols, map, "clo_code"),
                        GetOptionalValue(cols, map, "cognitive_level"),
                        exam.AssessmentType,
                        courseClos);

                    ValidateQuestion(request);

                    _db.Questions.Add(new ExamQuestion
                    {
                        InstitutionId = exam.InstitutionId,
                        ExamId = examId,
                        CourseLearningOutcomeId = request.CourseLearningOutcomeId,
                        CognitiveLevel = ParseCognitiveLevel(request.CognitiveLevel),
                        QuestionText = request.QuestionText ?? string.Empty,
            QuestionImageUrl = request.QuestionImageUrl,
                        ChoiceA = request.ChoiceA ?? string.Empty,
            ChoiceAImageUrl = request.ChoiceAImageUrl,
                        ChoiceB = request.ChoiceB ?? string.Empty,
            ChoiceBImageUrl = request.ChoiceBImageUrl,
                        ChoiceC = request.ChoiceC ?? string.Empty,
            ChoiceCImageUrl = request.ChoiceCImageUrl,
                        ChoiceD = request.ChoiceD ?? string.Empty,
            ChoiceDImageUrl = request.ChoiceDImageUrl,
                        CorrectAnswer = NormalizeAnswer(request.CorrectAnswer),
                        Explanation = request.Explanation
                    });

                    result.Inserted++;
                }
                catch (Exception ex)
                {
                    result.Skipped++;
                    result.Errors.Add(ex.Message);
                }
            }
        }
        else
        {
            throw new InvalidOperationException("Only .xlsx and .csv files are supported");
        }

        exam.BankQuestionCount += result.Inserted;

        if (exam.ExamQuestionCount == 0 && exam.BankQuestionCount > 0)
            exam.ExamQuestionCount = Math.Min(10, exam.BankQuestionCount);

        await _db.SaveChangesAsync(cancellationToken);
        return result;
    }

    public async Task<byte[]> BuildQuestionsTemplateAsync(Guid examId, CancellationToken cancellationToken = default)
    {
        var exam = await _db.Exams.AsNoTracking().FirstOrDefaultAsync(x => x.Id == examId, cancellationToken)
            ?? throw new InvalidOperationException("Exam not found");
        var courseClos = await _db.CourseLearningOutcomes.AsNoTracking()
            .Where(x => x.SubjectId == exam.SubjectId && x.InstitutionId == exam.InstitutionId && x.IsActive)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Code)
            .ToListAsync(cancellationToken);
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Questions");

        var headers = new[] { "question_text", "cognitive_level", "clo_code", "question_image_url", "choice_a", "choice_a_image_url", "choice_b", "choice_b_image_url", "choice_c", "choice_c_image_url", "choice_d", "choice_d_image_url", "correct_answer", "explanation" };
        for (var i = 0; i < headers.Length; i++) sheet.Cell(1, i + 1).Value = headers[i];

        sheet.Cell(2, 1).Value = "What is the capital of Egypt?";
        sheet.Cell(2, 2).Value = "Remember";
        sheet.Cell(2, 3).Value = courseClos.FirstOrDefault()?.Code ?? "";
        sheet.Cell(2, 5).Value = "Cairo";
        sheet.Cell(2, 7).Value = "Alexandria";
        sheet.Cell(2, 9).Value = "Mansoura";
        sheet.Cell(2, 11).Value = "Aswan";
        sheet.Cell(2, 13).Value = "A";
        sheet.Cell(2, 14).Value = "Cairo is the capital of Egypt.";

        var lists = workbook.Worksheets.Add("Allowed Values");
        lists.Cell(1, 1).Value = "cognitive_level";
        var levels = Enum.GetNames<CognitiveLevel>();
        for (var i = 0; i < levels.Length; i++) lists.Cell(i + 2, 1).Value = levels[i];
        lists.Cell(1, 2).Value = "clo_code (optional for General exams)";
        lists.Cell(1, 3).Value = "description";
        for (var i = 0; i < courseClos.Count; i++) { lists.Cell(i + 2, 2).Value = courseClos[i].Code; lists.Cell(i + 2, 3).Value = courseClos[i].Description; }
        lists.Cell(1, 5).Value = "Rule";
        lists.Cell(2, 5).Value = "cognitive_level is required for every question.";
        lists.Cell(3, 5).Value = exam.AssessmentType == AssessmentType.CloAligned ? "clo_code is required for this CLO-aligned exam." : "clo_code may be blank for questions not linked to a CLO.";
        lists.Columns().AdjustToContents();

        sheet.Range(1, 1, 1, headers.Length).Style.Font.Bold = true;
        sheet.Range(1, 1, 1, headers.Length).Style.Fill.BackgroundColor = XLColor.LightGreen;
        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void ValidateExamWindow(DateTime startAtUtc, DateTime endAtUtc)
    {
        if (endAtUtc <= startAtUtc)
            throw new InvalidOperationException("Exam end time must be after start time");
    }

    private static void ValidateCounts(int bankQuestionCount, int examQuestionCount)
    {
        if (bankQuestionCount < 0)
            throw new InvalidOperationException("Question bank number cannot be negative");

        if (examQuestionCount <= 0)
            throw new InvalidOperationException("Questions per paper must be greater than zero");

        // The paper blueprint may be prepared before its question bank is generated.
        // Availability is validated when publishing/starting the paper, not while planning it.
    }

    private static void ValidateQuestion(UpsertExamQuestionRequest request)
    {
        static bool HasValue(string? value) => !string.IsNullOrWhiteSpace(value);

        var hasQuestion =
            HasValue(request.QuestionText) ||
            HasValue(request.QuestionImageUrl);

        if (!hasQuestion)
            throw new InvalidOperationException("Question text or question image is required");

        if ((request.QuestionText ?? "").Contains("AI Question", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Placeholder question text is not allowed");

        if ((request.ChoiceA ?? "").Contains("Choice A", StringComparison.OrdinalIgnoreCase) ||
            (request.ChoiceB ?? "").Contains("Choice B", StringComparison.OrdinalIgnoreCase) ||
            (request.ChoiceC ?? "").Contains("Choice C", StringComparison.OrdinalIgnoreCase) ||
            (request.ChoiceD ?? "").Contains("Choice D", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Placeholder choices are not allowed");

        var hasChoiceA = HasValue(request.ChoiceA) || HasValue(request.ChoiceAImageUrl);
        var hasChoiceB = HasValue(request.ChoiceB) || HasValue(request.ChoiceBImageUrl);
        var hasChoiceC = HasValue(request.ChoiceC) || HasValue(request.ChoiceCImageUrl);
        var hasChoiceD = HasValue(request.ChoiceD) || HasValue(request.ChoiceDImageUrl);

        if (!hasChoiceA || !hasChoiceB || !hasChoiceC || !hasChoiceD)
            throw new InvalidOperationException("Each choice must have text or image");

        var answer = NormalizeAnswer(request.CorrectAnswer);
        if (answer is not ("A" or "B" or "C" or "D"))
            throw new InvalidOperationException("Correct answer must be one of A, B, C, D");
    }

    private static string NormalizeAnswer(string answer) =>
        (answer ?? string.Empty).Trim().ToUpperInvariant();

    private static AssessmentType ParseAssessmentType(string? value) =>
        Enum.TryParse<AssessmentType>(value, true, out var result) ? result : throw new InvalidOperationException("نوع الاختبار يجب أن يكون General أو CloAligned");

    private static CognitiveLevel ParseCognitiveLevel(string? value) =>
        Enum.TryParse<CognitiveLevel>(value, true, out var result) ? result : throw new InvalidOperationException("مستوى السؤال غير صحيح وفق تصنيف Bloom");

    private static void ApplyQuestionClassification(UpsertExamQuestionRequest request, string? cloCode, string? cognitiveLevel, AssessmentType assessmentType, IReadOnlyDictionary<string, Guid> courseClos)
    {
        request.CognitiveLevel = string.IsNullOrWhiteSpace(cognitiveLevel) ? throw new InvalidOperationException("cognitive_level is required") : cognitiveLevel.Trim();
        _ = ParseCognitiveLevel(request.CognitiveLevel);
        if (string.IsNullOrWhiteSpace(cloCode))
        {
            if (assessmentType == AssessmentType.CloAligned) throw new InvalidOperationException("clo_code is required for a CLO-aligned exam");
            request.CourseLearningOutcomeId = null;
            return;
        }
        if (!courseClos.TryGetValue(cloCode.Trim(), out var cloId)) throw new InvalidOperationException($"Unknown clo_code: {cloCode}");
        request.CourseLearningOutcomeId = cloId;
    }

    private static int ValidateMaxAttempts(int value)
    {
        if (value is < 1 or > 20) throw new InvalidOperationException("عدد المحاولات يجب أن يكون بين 1 و20");
        return value;
    }

    private static Guid RequireTenantInstitution(Guid? institutionId)
    {
        if (institutionId is { } value && value != Guid.Empty)
            return value;

        throw new UnauthorizedAccessException("Current user is not linked to an institution.");
    }

    private static Dictionary<string, int> BuildHeaderMap(List<string> headers)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < headers.Count; i++)
        {
            var key = headers[i].Trim();
            if (!string.IsNullOrWhiteSpace(key))
                map[key] = i;
        }

        if (!map.ContainsKey("correct_answer"))
            throw new InvalidOperationException("Missing required column: correct_answer");

        return map;
    }

    private static UpsertExamQuestionRequest ReadQuestionFromExcelRow(IXLRangeRow row, Dictionary<string, int> map)
    {
        string? ReadOptional(string key)
        {
            if (!map.ContainsKey(key))
                return null;

            var cellIndex = map[key] + 1;
            return row.Cell(cellIndex).GetString().Trim();
        }

        string ReadRequired(string key)
        {
            if (!map.ContainsKey(key))
                throw new InvalidOperationException($"Missing required column: {key}");

            var cellIndex = map[key] + 1;
            return row.Cell(cellIndex).GetString().Trim();
        }

        return new UpsertExamQuestionRequest
        {
            QuestionText = ReadOptional("question_text") ?? string.Empty,
            QuestionImageUrl = ReadOptional("question_image_url"),
            ChoiceA = ReadOptional("choice_a") ?? string.Empty,
            ChoiceAImageUrl = ReadOptional("choice_a_image_url"),
            ChoiceB = ReadOptional("choice_b") ?? string.Empty,
            ChoiceBImageUrl = ReadOptional("choice_b_image_url"),
            ChoiceC = ReadOptional("choice_c") ?? string.Empty,
            ChoiceCImageUrl = ReadOptional("choice_c_image_url"),
            ChoiceD = ReadOptional("choice_d") ?? string.Empty,
            ChoiceDImageUrl = ReadOptional("choice_d_image_url"),
            CorrectAnswer = ReadRequired("correct_answer"),
            Explanation = ReadOptional("explanation")
        };
    }

    private static string? ReadOptionalExcelValue(IXLRangeRow row, Dictionary<string, int> map, string key)
    {
        if (!map.TryGetValue(key, out var index)) return null;
        return row.Cell(index + 1).GetString().Trim();
    }

    private static string GetValue(string[] cols, Dictionary<string, int> map, string key)
    {
        if (!map.ContainsKey(key))
            throw new InvalidOperationException($"Missing required column: {key}");

        var index = map[key];
        if (index >= cols.Length)
            return string.Empty;

        return cols[index].Trim();
    }

    private static string? GetOptionalValue(string[] cols, Dictionary<string, int> map, string key)
    {
        if (!map.ContainsKey(key))
            return null;

        var index = map[key];
        if (index >= cols.Length)
            return null;

        return cols[index].Trim();
    }
}
