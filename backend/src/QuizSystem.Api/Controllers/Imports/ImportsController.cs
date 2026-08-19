using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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

namespace QuizSystem.Api.Controllers.Imports;
[ApiController]
[Route("api/imports")]
[Authorize(Policy = "AdminOrSupervisor")]
public class ImportsController : ControllerBase
{
    private readonly IExcelImportService _excelImportService;

    public ImportsController(IExcelImportService excelImportService)
    {
        _excelImportService = excelImportService;
    }

    [HttpGet("students/template")]
    public IActionResult StudentsTemplate()
    {
        var bytes = _excelImportService.BuildStudentsTemplate();
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "students_template.xlsx"
        );
    }

    [HttpPost("students")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> UploadStudents(IFormFile file, CancellationToken cancellationToken)
        => Ok(await _excelImportService.UploadStudentsAsync(file, cancellationToken));

    [HttpGet("registrations/template")]
    public IActionResult RegistrationsTemplate()
    {
        var bytes = _excelImportService.BuildRegistrationsTemplate();
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "registrations_template.xlsx"
        );
    }

    [HttpPost("registrations")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> UploadRegistrations(IFormFile file, CancellationToken cancellationToken)
        => Ok(await _excelImportService.UploadRegistrationsAsync(file, cancellationToken));
}
