using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizSystem.Infrastructure.Persistence;
using System.Security.Claims;

namespace QuizSystem.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/school")]
[Authorize(Policy = "AdminOnly")]
public class SchoolScopedDataController : ControllerBase
{
    private readonly AppDbContext _db;

    public SchoolScopedDataController(AppDbContext db)
    {
        _db = db;
    }

    private async Task<Guid?> GetCurrentInstitutionIdAsync(CancellationToken cancellationToken)
    {
        var rawUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(rawUserId, out var userId))
            return null;

        return await _db.Users
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => x.InstitutionId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    [HttpGet("students")]
    public async Task<IActionResult> GetInstitutionStudents(
        [FromQuery] string? grade,
        CancellationToken cancellationToken)
    {
        var institutionId = await GetCurrentInstitutionIdAsync(cancellationToken);
        if (institutionId is null)
            return Ok(Array.Empty<object>());

        var query = _db.Students
            .AsNoTracking()
            .Where(student => _db.Users.Any(user =>
                user.StudentProfileId == student.Id &&
                user.InstitutionId == institutionId));

        if (!string.IsNullOrWhiteSpace(grade))
        {
            var normalizedGrade = grade.Trim();
            query = query.Where(x => x.Grade == normalizedGrade);
        }

        var rows = await query
            .OrderBy(x => x.Grade)
            .ThenBy(x => x.FullName)
            .Select(x => new
            {
                id = x.Id,
                fullName = x.FullName,
                name = x.FullName,
                studentName = x.FullName,
                studentCode = x.StudentCode,
                code = x.StudentCode,
                grade = x.Grade,
                isActive = x.IsActive
            })
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }

    [HttpGet("parents")]
    public async Task<IActionResult> GetInstitutionParents(CancellationToken cancellationToken)
    {
        var institutionId = await GetCurrentInstitutionIdAsync(cancellationToken);
        if (institutionId is null)
            return Ok(Array.Empty<object>());

        var rows = await _db.Parents
            .AsNoTracking()
            .Where(parent =>
                _db.Users.Any(user =>
                    user.ParentProfileId == parent.Id &&
                    user.InstitutionId == institutionId) ||
                _db.ParentStudentLinks.Any(link =>
                    link.ParentProfileId == parent.Id &&
                    _db.Users.Any(user =>
                        user.StudentProfileId == link.StudentProfileId &&
                        user.InstitutionId == institutionId)))
            .OrderBy(x => x.FullName)
            .Select(x => new
            {
                id = x.Id,
                fullName = x.FullName,
                name = x.FullName,
                parentName = x.FullName,
                parentCode = x.ParentCode,
                code = x.ParentCode,
                phoneNumber = x.PhoneNumber,
                isActive = x.IsActive
            })
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }
}
