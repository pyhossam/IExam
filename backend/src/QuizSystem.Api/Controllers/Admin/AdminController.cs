using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizSystem.Infrastructure.Persistence;
using QuizSystem.Api.Infrastructure.Tenant;
using QuizSystem.Application.Contracts.Attempts;
using QuizSystem.Application.Contracts.Auth;
using QuizSystem.Application.Contracts.Exams;
using QuizSystem.Application.Contracts.Imports;
using QuizSystem.Application.Contracts.Portals;
using QuizSystem.Application.Contracts.Reports;
using QuizSystem.Application.DTOs;
using QuizSystem.Application.Interfaces;

using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace QuizSystem.Api.Controllers.Admin;
[ApiController]
[Route("api/admin")]
[Authorize(Policy = "AdminOnly")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    
    private readonly AppDbContext _db;
public AdminController(IAdminService adminService, AppDbContext db)
    {
        _adminService = adminService;
            _db = db;
}

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        var institutionId = await TenantResolver.RequireCurrentInstitutionIdAsync(_db, User, cancellationToken);
        return Ok(await _adminService.GetDashboardAsync(institutionId, cancellationToken));
    }

    
    [HttpPost("students")]
    public async Task<IActionResult> CreateStudent([FromBody] CreateStudentRequest request, CancellationToken cancellationToken)
    {
        var institutionId = await TenantResolver.RequireCurrentInstitutionIdAsync(_db, User, cancellationToken);
        return Ok(new { id = await _adminService.CreateStudentAsync(institutionId, request, cancellationToken) });
    }

    [HttpPost("parents")]
    public async Task<IActionResult> CreateParent([FromBody] CreateParentRequest request, CancellationToken cancellationToken)
    {
        var institutionId = await TenantResolver.RequireCurrentInstitutionIdAsync(_db, User, cancellationToken);
        return Ok(new { id = await _adminService.CreateParentAsync(institutionId, request, cancellationToken) });
    }

    [HttpPost("exams")]
    [Authorize(Policy = "AdminOrSupervisor")]
    public async Task<IActionResult> CreateExam([FromBody] CreateExamRequest request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var institutionId = await TenantResolver.RequireCurrentInstitutionIdAsync(_db, User, cancellationToken);
        return Ok(new { id = await _adminService.CreateExamAsync(institutionId, userId, request, cancellationToken) });
    }

    [HttpPost("exams/{examId:guid}/questions")]
    [Authorize(Policy = "AdminOrSupervisor")]
    public async Task<IActionResult> AddQuestion(Guid examId, [FromBody] AddQuestionRequest request, CancellationToken cancellationToken)
        => Ok(new { id = await _adminService.AddQuestionAsync(examId, request, cancellationToken) });

    [HttpPost("exams/{examId:guid}/registrations")]
    [Authorize(Policy = "AdminOrSupervisor")]
    public async Task<IActionResult> RegisterStudent(Guid examId, [FromBody] RegisterStudentRequest request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var institutionId = await TenantResolver.RequireCurrentInstitutionIdAsync(_db, User, cancellationToken);
        return Ok(new { id = await _adminService.RegisterStudentToExamAsync(institutionId, examId, request.StudentId, userId, cancellationToken) });
    }


    private async Task LinkProfileUserToCurrentInstitutionAsync(Guid? studentProfileId, Guid? parentProfileId, CancellationToken cancellationToken)
    {
        var currentUserRaw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(currentUserRaw, out var currentUserId))
            return;

        var institutionId = await _db.Users
            .Where(x => x.Id == currentUserId)
            .Select(x => x.InstitutionId)
            .FirstOrDefaultAsync(cancellationToken);

        if (institutionId is null)
            return;

        var user = await _db.Users.FirstOrDefaultAsync(x =>
            (studentProfileId != null && x.StudentProfileId == studentProfileId) ||
            (parentProfileId != null && x.ParentProfileId == parentProfileId),
            cancellationToken);

        if (user is null)
            return;

        user.InstitutionId = institutionId;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
