using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizSystem.Infrastructure.Persistence;
using QuizSystem.Api.Infrastructure.Tenant;
using QuizSystem.Application.Contracts.Users;
using QuizSystem.Application.DTOs;

namespace QuizSystem.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = "AdminOnly")]
public class UsersController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;

    
    private readonly AppDbContext _db;
public UsersController(IUserManagementService userManagementService, AppDbContext db)
    {
        _userManagementService = userManagementService;
            _db = db;
}

    [HttpGet]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
        => Ok(await _userManagementService.GetUsersAsync(await TenantResolver.GetCurrentInstitutionIdAsync(_db, User, cancellationToken), TenantResolver.IsSuperAdmin(User), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserManagementRequest request, CancellationToken cancellationToken)
        => Ok(await _userManagementService.CreateUserAsync(await TenantResolver.GetCurrentInstitutionIdAsync(_db, User, cancellationToken), TenantResolver.IsSuperAdmin(User), request, cancellationToken));

    [HttpPut("{userId:guid}")]
    public async Task<IActionResult> UpdateUser(Guid userId, [FromBody] UpdateUserManagementRequest request, CancellationToken cancellationToken)
        => Ok(await _userManagementService.UpdateUserAsync(await TenantResolver.GetCurrentInstitutionIdAsync(_db, User, cancellationToken), TenantResolver.IsSuperAdmin(User), userId, request, cancellationToken));

    [HttpPatch("{userId:guid}/status")]
    public async Task<IActionResult> ToggleStatus(Guid userId, [FromQuery] bool isActive, CancellationToken cancellationToken)
    {
        await _userManagementService.ToggleUserStatusAsync(await TenantResolver.GetCurrentInstitutionIdAsync(_db, User, cancellationToken), TenantResolver.IsSuperAdmin(User), userId, isActive, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> DeleteUser(Guid userId, CancellationToken cancellationToken)
    {
        await _userManagementService.DeleteUserAsync(await TenantResolver.GetCurrentInstitutionIdAsync(_db, User, cancellationToken), TenantResolver.IsSuperAdmin(User), userId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{userId:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid userId, [FromBody] AdminResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await _userManagementService.ResetPasswordAsync(await TenantResolver.GetCurrentInstitutionIdAsync(_db, User, cancellationToken), TenantResolver.IsSuperAdmin(User), userId, request, cancellationToken);
        return NoContent();
    }
}
