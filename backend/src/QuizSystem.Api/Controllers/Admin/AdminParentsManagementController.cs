using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizSystem.Api.Infrastructure.Tenant;
using QuizSystem.Domain.Entities;
using QuizSystem.Infrastructure.Persistence;

namespace QuizSystem.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/parents")]
[Authorize(Policy = "AdminOnly")]
public class AdminParentsManagementController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminParentsManagementController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetParents(CancellationToken cancellationToken)
    {
        var institutionId = await TenantResolver.RequireCurrentInstitutionIdAsync(_db, User, cancellationToken);

        var users = await _db.Set<AppUser>()
            .AsNoTracking()
            .Where(x => x.InstitutionId == institutionId)
            .Where(x => x.ParentProfileId != null)
            .Select(x => new
            {
                x.Id,
                x.ParentProfileId,
                x.UserName,
                x.IsActive,
                x.InstitutionId,
                InstitutionName = x.Institution != null ? x.Institution.Name : null
            })
            .ToListAsync(cancellationToken);

        var userMap = users
            .Where(x => x.ParentProfileId != null)
            .GroupBy(x => x.ParentProfileId!.Value)
            .ToDictionary(x => x.Key, x => x.First());

        var parents = await _db.Set<ParentProfile>()
            .AsNoTracking()
            .Where(x => x.InstitutionId == institutionId)
            .OrderBy(x => x.FullName)
            .Select(x => new
            {
                x.Id,
                x.FullName,
                x.ParentCode,
                x.PhoneNumber,
                x.IsActive,
                Students = x.ParentStudentLinks.Select(link => new
                {
                    id = link.StudentProfileId,
                    fullName = link.StudentProfile.FullName,
                    name = link.StudentProfile.FullName,
                    studentCode = link.StudentProfile.StudentCode,
                    code = link.StudentProfile.StudentCode,
                    grade = link.StudentProfile.Grade
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        var result = parents.Select(x =>
        {
            userMap.TryGetValue(x.Id, out var user);

            return new
            {
                id = x.Id,
                fullName = x.FullName,
                name = x.FullName,
                parentCode = x.ParentCode,
                code = x.ParentCode,
                phoneNumber = x.PhoneNumber,
                phone = x.PhoneNumber,
                userName = user?.UserName,
                userId = user?.Id,
                institutionId = user?.InstitutionId,
                institutionName = user?.InstitutionName,
                isActive = user?.IsActive ?? x.IsActive,
                childrenCount = x.Students.Count,
                studentIds = x.Students.Select(s => s.id).ToList(),
                students = x.Students
            };
        });

        return Ok(result);
    }

    [HttpPut("{parentId:guid}")]
    public async Task<IActionResult> UpdateParent(
        Guid parentId,
        [FromBody] UpdateParentManagementRequest request,
        CancellationToken cancellationToken)
    {
        var institutionId = await TenantResolver.RequireCurrentInstitutionIdAsync(_db, User, cancellationToken);

        var parent = await _db.Set<ParentProfile>()
            .FirstOrDefaultAsync(x => x.Id == parentId && x.InstitutionId == institutionId, cancellationToken);

        if (parent is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Parent not found",
                Detail = "ولي الأمر غير موجود",
                Status = StatusCodes.Status404NotFound
            });
        }

        parent.FullName = request.FullName?.Trim() ?? parent.FullName;
        parent.ParentCode = request.ParentCode?.Trim() ?? parent.ParentCode;
        parent.PhoneNumber = request.PhoneNumber?.Trim() ?? parent.PhoneNumber;
        parent.IsActive = request.IsActive;

        var user = await _db.Set<AppUser>()
            .FirstOrDefaultAsync(x => x.ParentProfileId == parentId && x.InstitutionId == institutionId, cancellationToken);

        if (user is not null)
        {
            if (!string.IsNullOrWhiteSpace(request.UserName))
                user.UserName = request.UserName.Trim();

            user.IsActive = request.IsActive;
        }

        var oldLinks = await _db.Set<ParentStudentLink>()
            .Where(x => x.ParentProfileId == parentId && x.InstitutionId == institutionId)
            .ToListAsync(cancellationToken);

        _db.Set<ParentStudentLink>().RemoveRange(oldLinks);

        if (request.StudentIds is not null && request.StudentIds.Count > 0)
        {
            var validStudentIds = await _db.Set<StudentProfile>()
                .Where(x => request.StudentIds.Contains(x.Id) && x.InstitutionId == institutionId)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            var newLinks = validStudentIds
                .Distinct()
                .Select(studentId => new ParentStudentLink
                {
                    Id = Guid.NewGuid(),
                    InstitutionId = institutionId,
                    ParentProfileId = parentId,
                    StudentProfileId = studentId
                })
                .ToList();

            _db.Set<ParentStudentLink>().AddRange(newLinks);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Parent updated successfully" });
    }

    [HttpPatch("{parentId:guid}/status")]
    public async Task<IActionResult> UpdateParentStatus(
        Guid parentId,
        [FromQuery] bool isActive,
        CancellationToken cancellationToken)
    {
        var institutionId = await TenantResolver.RequireCurrentInstitutionIdAsync(_db, User, cancellationToken);

        var parent = await _db.Set<ParentProfile>()
            .FirstOrDefaultAsync(x => x.Id == parentId && x.InstitutionId == institutionId, cancellationToken);

        if (parent is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Parent not found",
                Detail = "ولي الأمر غير موجود",
                Status = StatusCodes.Status404NotFound
            });
        }

        parent.IsActive = isActive;

        var user = await _db.Set<AppUser>()
            .FirstOrDefaultAsync(x => x.ParentProfileId == parentId && x.InstitutionId == institutionId, cancellationToken);

        if (user is not null)
            user.IsActive = isActive;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            id = parent.Id,
            isActive,
            message = isActive ? "Parent activated" : "Parent deactivated"
        });
    }

    [HttpDelete("{parentId:guid}")]
    public async Task<IActionResult> DeleteParent(Guid parentId, CancellationToken cancellationToken)
    {
        var institutionId = await TenantResolver.RequireCurrentInstitutionIdAsync(_db, User, cancellationToken);

        var parent = await _db.Set<ParentProfile>()
            .FirstOrDefaultAsync(x => x.Id == parentId && x.InstitutionId == institutionId, cancellationToken);

        if (parent is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Parent not found",
                Detail = "ولي الأمر غير موجود",
                Status = StatusCodes.Status404NotFound
            });
        }

        var links = await _db.Set<ParentStudentLink>()
            .Where(x => x.ParentProfileId == parentId && x.InstitutionId == institutionId)
            .ToListAsync(cancellationToken);

        var users = await _db.Set<AppUser>()
            .Where(x => x.ParentProfileId == parentId && x.InstitutionId == institutionId)
            .ToListAsync(cancellationToken);

        _db.Set<ParentStudentLink>().RemoveRange(links);
        _db.Set<AppUser>().RemoveRange(users);
        _db.Set<ParentProfile>().Remove(parent);

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Parent deleted successfully" });
    }
}

public class UpdateParentManagementRequest
{
    public string? FullName { get; set; }
    public string? ParentCode { get; set; }
    public string? PhoneNumber { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public List<Guid> StudentIds { get; set; } = new();
    public bool IsActive { get; set; } = true;
}
