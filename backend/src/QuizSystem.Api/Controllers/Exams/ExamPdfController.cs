using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizSystem.Application.Contracts.Attempts;
using QuizSystem.Application.Contracts.Auth;
using QuizSystem.Application.Contracts.Exams;
using QuizSystem.Application.Contracts.Imports;
using QuizSystem.Application.Contracts.Portals;
using QuizSystem.Application.Contracts.Reports;
using QuizSystem.Application.DTOs;
using QuizSystem.Infrastructure.Persistence;
using QuizSystem.Infrastructure.Services;

namespace QuizSystem.Api.Controllers.Exams;
[ApiController]
[Route("api/exams")]
[Authorize(Policy = "AdminOrSupervisor")]
public class ExamPdfController : ControllerBase
{
    private readonly IExamPdfService _examPdfService;

    public ExamPdfController(IExamPdfService examPdfService)
    {
        _examPdfService = examPdfService;
    }

    [HttpGet("{examId:guid}/pdf/questions")]
    public async Task<IActionResult> QuestionsPdf(Guid examId, [FromQuery] bool withAnswers = false, CancellationToken cancellationToken = default)
    {
        var bytes = await _examPdfService.ExportQuestionsPdfAsync(examId, withAnswers, cancellationToken);
        var fileName = withAnswers ? "questions_with_answers.pdf" : "questions_without_answers.pdf";
        return File(bytes, "application/pdf", fileName);
    }

    [HttpGet("{examId:guid}/pdf/random-forms")]
    public async Task<IActionResult> RandomFormsPdf(Guid examId, [FromQuery] int formsCount = 3, CancellationToken cancellationToken = default)
    {
        var bytes = await _examPdfService.ExportRandomFormsPdfAsync(examId, formsCount, cancellationToken);
        return File(bytes, "application/zip", $"exam_forms_{formsCount}.zip");
    }

    [HttpGet("{examId:guid}/pdf/random-forms-answer-keys")]
    public async Task<IActionResult> RandomFormsAnswerKeysPdf(Guid examId, [FromQuery] int formsCount = 3, CancellationToken cancellationToken = default)
    {
        var bytes = await _examPdfService.ExportRandomFormsAnswerKeysPdfAsync(examId, formsCount, cancellationToken);
        return File(bytes, "application/zip", $"exam_answer_keys_{formsCount}.zip");
    }
}
