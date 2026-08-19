using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizSystem.Application.Contracts.Auth;
using QuizSystem.Domain.Entities;
using QuizSystem.Domain.Enums;
using QuizSystem.Infrastructure.Persistence;

namespace QuizSystem.Api.Controllers.SuperAdmin;

[ApiController]
[Route("api/super-admin")]
[Authorize(Policy = "SuperAdminOnly")]
public class SuperAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public SuperAdminController(AppDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        var data = await _db.Institutions
            .AsNoTracking()
            .Select(i => new
            {
                institutionId = i.Id,
                i.Name,
                i.Type,
                i.IsActive,
                admins = i.Users.Count(u => u.Role == UserRole.InstitutionAdmin),
                users = i.Users.Count,
                students = _db.Students.Count(s => s.InstitutionId == i.Id),
                parents = _db.Parents.Count(p => p.InstitutionId == i.Id),
                teachers = _db.Teachers.Count(t => t.InstitutionId == i.Id),
                gradeLevels = _db.GradeLevels.Count(g => g.InstitutionId == i.Id),
                subjects = _db.Subjects.Count(s => s.InstitutionId == i.Id),
                classSections = _db.ClassSections.Count(s => s.InstitutionId == i.Id),
                exams = _db.Exams.Count(e => e.InstitutionId == i.Id),
                completedAttempts = _db.Attempts.Count(a => a.InstitutionId == i.Id && a.SubmittedAtUtc != null)
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            institutionsCount = data.Count,
            activeInstitutionsCount = data.Count(x => x.IsActive),
            totalStudents = data.Sum(x => x.students),
            totalTeachers = data.Sum(x => x.teachers),
            totalExams = data.Sum(x => x.exams),
            institutions = data
        });
    }

    [HttpGet("institutions")]
    public async Task<IActionResult> GetInstitutions(CancellationToken cancellationToken)
        => Ok(await _db.Institutions.AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Type,
                x.Address,
                x.PhoneNumber,
                x.Email,
                x.LogoUrl,
                x.IsActive,
                x.ExamManagementMode,
                x.CreatedAtUtc,
                Admins = x.Users
                    .Where(u => u.Role == UserRole.InstitutionAdmin)
                    .OrderBy(u => u.UserName)
                    .Select(u => new { u.Id, u.UserName, u.Email, u.IsActive })
                    .ToList()
            })
            .ToListAsync(cancellationToken));

    [HttpPost("institutions")]
    public async Task<IActionResult> CreateInstitution([FromBody] CreateInstitutionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new ProblemDetails { Title = "Institution name is required" });

        var entity = new Institution
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Type = request.Type,
            Address = request.Address,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            LogoUrl = request.LogoUrl,
            IsActive = request.IsActive,
            ExamManagementMode = request.ExamManagementMode ?? ExamManagementMode.TeachersAndExamSupervisors,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Institutions.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(entity);
    }

    [HttpPut("institutions/{institutionId:guid}")]
    public async Task<IActionResult> UpdateInstitution(Guid institutionId, [FromBody] UpdateInstitutionRequest request, CancellationToken cancellationToken)
    {
        var entity = await _db.Institutions.FirstOrDefaultAsync(x => x.Id == institutionId, cancellationToken);
        if (entity is null) return NotFound();

        entity.Name = string.IsNullOrWhiteSpace(request.Name) ? entity.Name : request.Name.Trim();
        entity.Type = request.Type;
        entity.Address = request.Address;
        entity.PhoneNumber = request.PhoneNumber;
        entity.Email = request.Email;
        entity.LogoUrl = request.LogoUrl;
        entity.IsActive = request.IsActive;
        entity.ExamManagementMode = request.ExamManagementMode;

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(entity);
    }

    [HttpPatch("institutions/{institutionId:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid institutionId, [FromQuery] bool isActive, CancellationToken cancellationToken)
    {
        var affected = await _db.Institutions
            .Where(x => x.Id == institutionId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, isActive), cancellationToken);

        return affected == 0 ? NotFound() : NoContent();
    }

    [HttpPost("institutions/{institutionId:guid}/admins")]
    public async Task<IActionResult> CreateInstitutionAdmin(Guid institutionId, [FromBody] CreateInstitutionAdminRequest request, CancellationToken cancellationToken)
    {
        var exists = await _db.Institutions.AnyAsync(x => x.Id == institutionId, cancellationToken);
        if (!exists) return NotFound(new ProblemDetails { Title = "Institution not found" });

        if (await _db.Users.AnyAsync(x => x.UserName == request.UserName, cancellationToken))
            return Conflict(new ProblemDetails { Title = "UserName already exists" });

        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim().ToLowerInvariant();
        if (email is not null && await _db.Users.AnyAsync(x => x.Email == email, cancellationToken))
            return Conflict(new ProblemDetails { Title = "Email already exists" });

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            InstitutionId = institutionId,
            UserName = request.UserName.Trim(),
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = UserRole.InstitutionAdmin,
            IsActive = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { user.Id, user.UserName, user.Role, user.InstitutionId });
    }

    [HttpPut("institutions/{institutionId:guid}/admins/{adminId:guid}")]
    public async Task<IActionResult> UpdateInstitutionAdmin(
        Guid institutionId,
        Guid adminId,
        [FromBody] UpdateInstitutionAdminRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(
            x => x.Id == adminId && x.InstitutionId == institutionId && x.Role == UserRole.InstitutionAdmin,
            cancellationToken);
        if (user is null) return NotFound(new ProblemDetails { Title = "Institution administrator not found" });

        var userName = request.UserName?.Trim();
        if (string.IsNullOrWhiteSpace(userName))
            return BadRequest(new ProblemDetails { Title = "User name is required" });
        if (await _db.Users.AnyAsync(x => x.Id != adminId && x.UserName == userName, cancellationToken))
            return Conflict(new ProblemDetails { Title = "UserName already exists" });

        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim().ToLowerInvariant();
        if (email is not null && await _db.Users.AnyAsync(x => x.Id != adminId && x.Email == email, cancellationToken))
            return Conflict(new ProblemDetails { Title = "Email already exists" });

        user.UserName = userName;
        user.Email = email;
        user.IsActive = request.IsActive;
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            if (request.Password.Length < 8)
                return BadRequest(new ProblemDetails { Title = "Password must contain at least 8 characters" });
            user.PasswordHash = _passwordHasher.Hash(request.Password);
            user.MustChangePassword = request.MustChangePassword;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { user.Id, user.UserName, user.Email, user.IsActive, user.InstitutionId });
    }
}

public sealed class CreateInstitutionRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public ExamManagementMode? ExamManagementMode { get; set; }
}

public sealed class UpdateInstitutionRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public ExamManagementMode ExamManagementMode { get; set; } = ExamManagementMode.TeachersAndExamSupervisors;
}

public sealed class CreateInstitutionAdminRequest
{
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Password { get; set; } = string.Empty;
}

public sealed class UpdateInstitutionAdminRequest
{
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Password { get; set; }
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; } = true;
}
