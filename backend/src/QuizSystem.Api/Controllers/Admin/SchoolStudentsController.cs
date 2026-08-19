using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizSystem.Infrastructure.Persistence;
using System.Security.Claims;

namespace QuizSystem.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/school")]
[Authorize]
[NonController]
public class SchoolStudentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public SchoolStudentsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("students")]
    public async Task<IActionResult> GetInstitutionStudents(CancellationToken cancellationToken)
    {
        var currentUserIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(currentUserIdRaw, out var currentUserId))
            return Unauthorized(new { message = "Current user id was not found in token." });

        var currentUser = await _db.Users
            .AsNoTracking()
            .Where(x => x.Id == currentUserId)
            .Select(x => new
            {
                x.Id,
                x.InstitutionId,
                Role = x.Role.ToString()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (currentUser is null)
            return Unauthorized(new { message = "Current user was not found." });

        var isSuperAdmin = string.Equals(currentUser.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);

        var query = _db.Students.AsNoTracking();

        // InstitutionAdmin / SchoolAdmin / Admin must see only students linked to users in the same institution.
        // SuperAdmin can see all students.
        if (!isSuperAdmin)
        {
            if (currentUser.InstitutionId is null)
                return Ok(Array.Empty<object>());

            var institutionId = currentUser.InstitutionId.Value;

            query = query.Where(student =>
                _db.Users.Any(user =>
                    user.StudentProfileId == student.Id &&
                    user.InstitutionId == institutionId));
        }

        var rows = await query
            .OrderBy(x => x.FullName)
            .Select(x => new
            {
                id = x.Id,
                name = x.FullName,
                fullName = x.FullName,
                code = x.StudentCode,
                studentCode = x.StudentCode,
                grade = x.Grade,
                isActive = x.IsActive
            })
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }
}
