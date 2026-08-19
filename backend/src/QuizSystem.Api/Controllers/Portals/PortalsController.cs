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
[Route("api/portal")]
public class PortalsController : ControllerBase
{
    private readonly IPortalService _portalService;

    public PortalsController(IPortalService portalService)
    {
        _portalService = portalService;
    }

    [HttpGet("student/dashboard")]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> StudentDashboard(CancellationToken cancellationToken)
    {
        var studentId = Guid.Parse(User.FindFirstValue("studentProfileId")!);
        return Ok(await _portalService.GetStudentDashboardAsync(studentId, cancellationToken));
    }

    [HttpGet("parent/dashboard")]
    [Authorize(Policy = "ParentOnly")]
    public async Task<IActionResult> ParentDashboard(CancellationToken cancellationToken)
    {
        var parentId = Guid.Parse(User.FindFirstValue("parentProfileId")!);
        return Ok(await _portalService.GetParentDashboardAsync(parentId, cancellationToken));
    }

    [HttpGet("exams/{examId:guid}/leaderboard")]
    [Authorize(Policy = "AdminOrSupervisor")]
    public async Task<IActionResult> Leaderboard(Guid examId, CancellationToken cancellationToken)
        => Ok(await _portalService.GetExamLeaderboardAsync(examId, cancellationToken));
}
