using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuizSystem.Api.Infrastructure.Tenant;
using QuizSystem.Application.Contracts.Attempts;
using QuizSystem.Application.Contracts.Auth;
using QuizSystem.Application.Contracts.Exams;
using QuizSystem.Application.Contracts.Imports;
using QuizSystem.Application.Contracts.Portals;
using QuizSystem.Application.Contracts.Reports;
using QuizSystem.Application.DTOs;
using QuizSystem.Infrastructure.Persistence;
using QuizSystem.Infrastructure.Services;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using UglyToad.PdfPig;

namespace QuizSystem.Api.Controllers.Exams;
[ApiController]
[Route("api/exams")]
[Authorize(Policy = "AdminOrSupervisor")]
public class ExamsController : ControllerBase
{
    private readonly IExamManagementService _examManagementService;
    private readonly IAiQuestionGenerator _aiQuestionGenerator;

    
    private readonly AppDbContext _db;
public ExamsController(IExamManagementService examManagementService, IAiQuestionGenerator aiQuestionGenerator, AppDbContext db)
    {
        _examManagementService = examManagementService;
        _aiQuestionGenerator = aiQuestionGenerator;
            _db = db;
}

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("ai")]
    public async Task<IActionResult> CreateAiExam([FromBody] CreateAiExamRequest request, CancellationToken cancellationToken)
    { await RequireSubjectAccess(request.SubjectId, cancellationToken); return Ok(new { id = await _examManagementService.CreateAiExamAsync(await TenantResolver.RequireCurrentInstitutionIdAsync(_db, User, cancellationToken), CurrentUserId, request, cancellationToken) }); }

    [HttpPost("manual")]
    public async Task<IActionResult> CreateManualExam([FromBody] CreateManualExamRequest request, CancellationToken cancellationToken)
    { await RequireSubjectAccess(request.SubjectId, cancellationToken); return Ok(new { id = await _examManagementService.CreateManualExamAsync(await TenantResolver.RequireCurrentInstitutionIdAsync(_db, User, cancellationToken), CurrentUserId, request, cancellationToken) }); }

    [HttpGet]
    public async Task<IActionResult> GetExams(CancellationToken cancellationToken)
    { var rows = await _examManagementService.GetExamsAsync(await TenantResolver.GetCurrentInstitutionIdAsync(_db, User, cancellationToken), TenantResolver.IsSuperAdmin(User), cancellationToken); if (User.IsInRole("CourseSupervisor") || User.IsInRole("Teacher")) { var ids = await AssignedSubjectIds(cancellationToken); rows = rows.Where(x => x.SubjectId.HasValue && ids.Contains(x.SubjectId.Value)).ToList(); } return Ok(rows); }

    [HttpGet("{examId:guid}")]
    public async Task<IActionResult> GetExamDetails(Guid examId, CancellationToken cancellationToken)
    { await RequireExamAccess(examId, cancellationToken); return Ok(await _examManagementService.GetExamDetailsAsync(await TenantResolver.GetCurrentInstitutionIdAsync(_db, User, cancellationToken), TenantResolver.IsSuperAdmin(User), examId, cancellationToken)); }

    [HttpPut("{examId:guid}/settings")]
    public async Task<IActionResult> UpdateSettings(Guid examId, [FromBody] UpdateExamSettingsRequest request, CancellationToken cancellationToken)
    {
        await RequireExamAccess(examId, cancellationToken);
        await _examManagementService.UpdateExamSettingsAsync(examId, request, cancellationToken);
        return Ok(new { message = "Exam settings updated successfully" });
    }

    [HttpPost("{examId:guid}/questions")]
    public async Task<IActionResult> AddQuestion(Guid examId, [FromBody] UpsertExamQuestionRequest request, CancellationToken cancellationToken)
    { await RequireExamAccess(examId, cancellationToken); await ValidateClo(examId, request.CourseLearningOutcomeId, cancellationToken); return Ok(new { id = await _examManagementService.AddQuestionAsync(examId, request, cancellationToken) }); }

    [HttpPut("questions/{questionId:guid}")]
    public async Task<IActionResult> UpdateQuestion(Guid questionId, [FromBody] UpsertExamQuestionRequest request, CancellationToken cancellationToken)
    {
        var examId = await _db.Questions.Where(x => x.Id == questionId).Select(x => x.ExamId).FirstOrDefaultAsync(cancellationToken); await RequireExamAccess(examId, cancellationToken); await ValidateClo(examId, request.CourseLearningOutcomeId, cancellationToken);
        await _examManagementService.UpdateQuestionAsync(questionId, request, cancellationToken);
        return Ok(new { message = "Question updated successfully" });
    }

    [HttpDelete("questions/{questionId:guid}")]
    public async Task<IActionResult> DeleteQuestion(Guid questionId, CancellationToken cancellationToken)
    {
        var examId = await _db.Questions.Where(x => x.Id == questionId).Select(x => x.ExamId).FirstOrDefaultAsync(cancellationToken); await RequireExamAccess(examId, cancellationToken);
        await _examManagementService.DeleteQuestionAsync(questionId, cancellationToken);
        return Ok(new { message = "Question deleted successfully" });
    }

    [HttpGet("{examId:guid}/questions/template")]
    public async Task<IActionResult> DownloadTemplate(Guid examId, CancellationToken cancellationToken)
    {
        await RequireExamAccess(examId, cancellationToken);
        var bytes = await _examManagementService.BuildQuestionsTemplateAsync(examId, cancellationToken);
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"exam_{examId}_questions_template.xlsx"
        );
    }

    [HttpPost("{examId:guid}/questions/upload")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> UploadQuestions(Guid examId, IFormFile file, CancellationToken cancellationToken)
    {
        await RequireExamAccess(examId, cancellationToken);
        var result = await _examManagementService.UploadQuestionsAsync(examId, file, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{examId:guid}/questions/ai-preview")]
    [RequestSizeLimit(30_000_000)]
    public async Task<IActionResult> GenerateAiQuestionPreview(Guid examId, [FromForm] int count, [FromForm] IFormFile? file, CancellationToken cancellationToken)
    {
        await RequireExamAccess(examId, cancellationToken);
        if (count is < 1 or > 50) throw new InvalidOperationException("عدد الأسئلة المطلوب يجب أن يكون بين 1 و50");
        if (file is not null && !Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("المحتوى التعليمي يجب أن يكون ملف PDF");

        var exam = await _db.Exams.AsNoTracking().FirstAsync(x => x.Id == examId, cancellationToken);
        var clos = exam.SubjectId.HasValue
            ? await _db.CourseLearningOutcomes.AsNoTracking().Where(x => x.SubjectId == exam.SubjectId && x.InstitutionId == exam.InstitutionId && x.IsActive).ToListAsync(cancellationToken)
            : [];
        var subject = exam.SubjectId.HasValue
            ? await _db.Subjects.AsNoTracking()
                .Where(x => x.Id == exam.SubjectId.Value && x.InstitutionId == exam.InstitutionId)
                .Select(x => new { x.Name, x.Code })
                .FirstOrDefaultAsync(cancellationToken)
            : null;
        var topic = $"المقرر: {subject?.Name ?? "غير محدد"} ({subject?.Code ?? "بدون كود"})\nعنوان الاختبار: {exam.Title}\nموضوع الاختبار: {exam.Topic}\nوصف الاختبار: {exam.Description}";
        string? summarizedContent = null;
        if (file is not null)
        {
            var extractedContent = await ExtractPdfText(file, cancellationToken);
            summarizedContent = await _aiQuestionGenerator.SummarizeEducationalContentAsync(
                extractedContent,
                topic,
                cancellationToken);
        }
        var cloCodes = clos.ToDictionary(x => x.Id.ToString(), x => x.Code);
        var batches = Enumerable.Range(0, (int)Math.Ceiling(count / 12m))
            .Select(index => Math.Min(12, count - index * 12))
            .ToList();
        var tasks = batches.Select((batchCount, index) =>
        {
            return _aiQuestionGenerator.GenerateQuestionsAsync(
                topic,
                batchCount,
                summarizedContent,
                BuildBlueprintInstructions(exam, cloCodes, batchCount, count, index + 1, batches.Count),
                cancellationToken);
        });
        var generated = (await Task.WhenAll(tasks)).SelectMany(x => x).Take(count).ToList();
        var cloByCode = clos.ToDictionary(x => x.Code, x => x.Id, StringComparer.OrdinalIgnoreCase);

        return Ok(generated.Take(count).Select(q => new UpsertExamQuestionRequest
        {
            CourseLearningOutcomeId = q.CloCode is not null && cloByCode.TryGetValue(q.CloCode, out var cloId) ? cloId : null,
            CognitiveLevel = q.CognitiveLevel,
            QuestionText = q.QuestionText,
            ChoiceA = q.ChoiceA, ChoiceB = q.ChoiceB, ChoiceC = q.ChoiceC, ChoiceD = q.ChoiceD,
            CorrectAnswer = q.CorrectAnswer,
            Explanation = q.Explanation
        }));
    }

    private static async Task<string> ExtractPdfText(IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        using var document = PdfDocument.Open(stream);
        var text = new StringBuilder();
        foreach (var page in document.GetPages())
        {
            ct.ThrowIfCancellationRequested();
            text.AppendLine(page.Text);
            if (text.Length >= 30_000) break;
        }
        var result = text.ToString();
        if (string.IsNullOrWhiteSpace(result)) throw new InvalidOperationException("تعذر استخراج نص من ملف PDF؛ تأكد أن الملف يحتوي على نص قابل للقراءة وليس صورًا ممسوحة فقط");
        return result[..Math.Min(result.Length, 30_000)];
    }

    private static string BuildBlueprintInstructions(QuizSystem.Domain.Entities.Exam exam, IReadOnlyDictionary<string, string> cloCodes, int batchCount, int requestedCount, int batchNumber, int batchesCount)
    {
        static Dictionary<string, int> Parse(string json) { try { return JsonSerializer.Deserialize<Dictionary<string, int>>(json) ?? []; } catch { return []; } }
        var paperTotal = Math.Max(1, exam.ExamQuestionCount);
        string Allocation(Dictionary<string, int> values, Func<string, string> label) => string.Join(", ", values.Where(x => x.Value > 0).Select(x =>
        {
            var percentage = Math.Round(x.Value * 100m / paperTotal, 1);
            var batchAllocation = Math.Max(0, (int)Math.Round(batchCount * percentage / 100m));
            return $"{label(x.Key)}: {percentage}% (نحو {batchAllocation} سؤال في هذه الدفعة)";
        }));
        var clo = Allocation(Parse(exam.BlueprintCloDistributionJson), key => key == "none" ? "بدون CLO" : cloCodes.GetValueOrDefault(key, key));
        var bloom = Allocation(Parse(exam.BlueprintBloomDistributionJson), key => key);
        return $"الدفعة {batchNumber} من {batchesCount}، وعددها {batchCount} من إجمالي {requestedCount}. نوع الاختبار: {exam.AssessmentType}. نسب CLO: {clo}. نسب Bloom: {bloom}. التزم بهذه النسب قدر الإمكان ولا تكرر سؤالًا داخل الدفعة.";
    }

    private async Task<HashSet<Guid>> AssignedSubjectIds(CancellationToken ct)
    {
        var tenant = await TenantResolver.RequireCurrentInstitutionIdAsync(_db, User, ct); var teacherId = await _db.Users.Where(x => x.Id == CurrentUserId && x.InstitutionId == tenant).Select(x => x.TeacherProfileId).FirstOrDefaultAsync(ct);
        if (!teacherId.HasValue) return [];
        if (User.IsInRole("Teacher"))
            return (await _db.ClassSections.Where(x => x.InstitutionId == tenant && x.TeacherProfileId == teacherId && x.IsActive).Select(x => x.SubjectId).Distinct().ToListAsync(ct)).ToHashSet();
        return (await _db.TeacherSubjects.Where(x => x.InstitutionId == tenant && x.TeacherProfileId == teacherId && x.IsActive).Select(x => x.SubjectId).ToListAsync(ct)).ToHashSet();
    }
    private async Task RequireSubjectAccess(Guid? subjectId, CancellationToken ct)
    {
        if (!subjectId.HasValue) throw new InvalidOperationException("اختيار المقرر مطلوب");
        var tenant = await TenantResolver.RequireCurrentInstitutionIdAsync(_db, User, ct); if (!await _db.Subjects.AnyAsync(x => x.Id == subjectId && x.InstitutionId == tenant, ct)) throw new KeyNotFoundException("المقرر غير موجود");
        if ((User.IsInRole("CourseSupervisor") || User.IsInRole("Teacher")) && !(await AssignedSubjectIds(ct)).Contains(subjectId.Value)) throw new UnauthorizedAccessException("ليس لديك تكليف نشط على هذا المقرر");
    }
    private async Task RequireExamAccess(Guid examId, CancellationToken ct)
    {
        var exam = await _db.Exams.AsNoTracking().FirstOrDefaultAsync(x => x.Id == examId, ct) ?? throw new KeyNotFoundException("الاختبار غير موجود"); await RequireSubjectAccess(exam.SubjectId, ct);
    }
    private async Task ValidateClo(Guid examId, Guid? cloId, CancellationToken ct)
    {
        var exam = await _db.Exams.AsNoTracking().FirstAsync(x => x.Id == examId, ct);
        if (exam.AssessmentType == QuizSystem.Domain.Enums.AssessmentType.CloAligned && !cloId.HasValue) throw new InvalidOperationException("يجب ربط كل سؤال بمخرج تعلم في الاختبار المرتبط بـ CLO");
        if (cloId.HasValue && !await _db.CourseLearningOutcomes.AnyAsync(x => x.Id == cloId && x.SubjectId == exam.SubjectId && x.InstitutionId == exam.InstitutionId && x.IsActive, ct)) throw new InvalidOperationException("مخرج التعلم لا يتبع مقرر الاختبار");
    }
}
