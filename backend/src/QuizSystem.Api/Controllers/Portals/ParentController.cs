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
using System.Security.Claims;

namespace QuizSystem.Api.Controllers.Portals;
[ApiController]
[Route("api/parent")]
[Authorize(Policy = "ParentOnly")]
public class ParentController : ControllerBase
{
    private readonly IStudentExamService _studentExamService;

    public ParentController(IStudentExamService studentExamService)
    {
        _studentExamService = studentExamService;
    }

    [HttpGet("children/results")]
    public async Task<IActionResult> Results(CancellationToken cancellationToken)
    {
        var parentProfileId = Guid.Parse(User.FindFirstValue("parentProfileId")!);
        return Ok(await _studentExamService.GetChildrenResultsAsync(parentProfileId, cancellationToken));
    }
}
