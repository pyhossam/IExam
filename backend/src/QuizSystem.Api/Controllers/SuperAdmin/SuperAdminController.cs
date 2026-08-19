using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizSystem.Application.Contracts.Auth;
using QuizSystem.Domain.Entities;
using QuizSystem.Domain.Enums;
using QuizSystem.Infrastructure.Persistence;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Security.Cryptography;

namespace QuizSystem.Api.Controllers.SuperAdmin;

[ApiController]
[Route("api/super-admin")]
[Authorize(Policy = "SuperAdminOnly")]
public class SuperAdminController : ControllerBase
{
    private static readonly ConcurrentDictionary<Guid, ResetChallenge> ResetChallenges = new();
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
                admins = i.Users.Count(u => u.Role == UserRole.InstitutionAdmin || u.Role == UserRole.Admin),
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
                    .Where(u => u.Role == UserRole.InstitutionAdmin || u.Role == UserRole.Admin)
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
            x => x.Id == adminId && x.InstitutionId == institutionId &&
                 (x.Role == UserRole.InstitutionAdmin || x.Role == UserRole.Admin),
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

    [HttpPost("data-reset/challenge")]
    public async Task<IActionResult> CreateDataResetChallenge([FromBody] CreateDataResetChallengeRequest request, CancellationToken cancellationToken)
    {
        string targetName;
        if (request.InstitutionId.HasValue)
        {
            targetName = await _db.Institutions
                .Where(x => x.Id == request.InstitutionId.Value)
                .Select(x => x.Name)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new KeyNotFoundException("المؤسسة المحددة غير موجودة.");
        }
        else
        {
            targetName = "جميع المؤسسات";
        }

        var challengeId = Guid.NewGuid();
        var verificationCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(5);
        ResetChallenges[challengeId] = new ResetChallenge(
            CurrentSuperAdminId(), request.InstitutionId, verificationCode, expiresAtUtc);

        RemoveExpiredChallenges();
        return Ok(new { challengeId, verificationCode, expiresAtUtc, targetName });
    }

    [HttpPost("data-reset")]
    public async Task<IActionResult> ResetData([FromBody] ResetDataRequest request, CancellationToken cancellationToken)
    {
        if (!ResetChallenges.TryRemove(request.ChallengeId, out var challenge) ||
            challenge.UserId != CurrentSuperAdminId() ||
            challenge.InstitutionId != request.InstitutionId ||
            challenge.ExpiresAtUtc < DateTime.UtcNow ||
            !CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(challenge.VerificationCode),
                System.Text.Encoding.UTF8.GetBytes(request.VerificationCode?.Trim() ?? string.Empty)))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "رمز التحقق غير صحيح أو انتهت صلاحيته. أنشئ رمزاً جديداً وحاول مرة أخرى."
            });
        }

        var institutionIds = request.InstitutionId.HasValue
            ? new[] { request.InstitutionId.Value }
            : await _db.Institutions.Select(x => x.Id).ToArrayAsync(cancellationToken);

        if (request.InstitutionId.HasValue && !await _db.Institutions.AnyAsync(x => x.Id == request.InstitutionId.Value, cancellationToken))
            return NotFound(new ProblemDetails { Title = "المؤسسة المحددة غير موجودة." });

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var deletedAttempts = await _db.Attempts.Where(x => institutionIds.Contains(x.InstitutionId)).CountAsync(cancellationToken);
        var deletedExams = await _db.Exams.Where(x => institutionIds.Contains(x.InstitutionId)).CountAsync(cancellationToken);
        var deletedStudents = await _db.Students.Where(x => institutionIds.Contains(x.InstitutionId)).CountAsync(cancellationToken);
        var deletedUsers = await _db.Users.Where(x => x.InstitutionId.HasValue && institutionIds.Contains(x.InstitutionId.Value) &&
            x.Role != UserRole.Admin && x.Role != UserRole.InstitutionAdmin).CountAsync(cancellationToken);

        await _db.ExamAttemptDraftAnswers.Where(x => institutionIds.Contains(x.InstitutionId)).ExecuteDeleteAsync(cancellationToken);
        await _db.ExamAttemptViolations.Where(x => institutionIds.Contains(x.InstitutionId)).ExecuteDeleteAsync(cancellationToken);
        await _db.AttemptAnswers.Where(x => institutionIds.Contains(x.InstitutionId)).ExecuteDeleteAsync(cancellationToken);
        await _db.ExamAttemptQuestionSnapshots.Where(x => institutionIds.Contains(x.InstitutionId)).ExecuteDeleteAsync(cancellationToken);
        await _db.Attempts.Where(x => institutionIds.Contains(x.InstitutionId)).ExecuteDeleteAsync(cancellationToken);
        await _db.Registrations.Where(x => institutionIds.Contains(x.InstitutionId)).ExecuteDeleteAsync(cancellationToken);
        await _db.Questions.Where(x => institutionIds.Contains(x.InstitutionId)).ExecuteDeleteAsync(cancellationToken);
        await _db.Exams.Where(x => institutionIds.Contains(x.InstitutionId)).ExecuteDeleteAsync(cancellationToken);
        await _db.SectionStudents.Where(x => institutionIds.Contains(x.InstitutionId)).ExecuteDeleteAsync(cancellationToken);
        await _db.ParentStudentLinks.Where(x => institutionIds.Contains(x.InstitutionId)).ExecuteDeleteAsync(cancellationToken);
        await _db.TeacherSubjects.Where(x => institutionIds.Contains(x.InstitutionId)).ExecuteDeleteAsync(cancellationToken);
        await _db.CourseLearningOutcomes.Where(x => institutionIds.Contains(x.InstitutionId)).ExecuteDeleteAsync(cancellationToken);
        await _db.ClassSections.Where(x => institutionIds.Contains(x.InstitutionId)).ExecuteDeleteAsync(cancellationToken);
        await _db.Subjects.Where(x => institutionIds.Contains(x.InstitutionId)).ExecuteDeleteAsync(cancellationToken);
        await _db.GradeLevels.Where(x => institutionIds.Contains(x.InstitutionId)).ExecuteDeleteAsync(cancellationToken);
        await _db.StudentAccountRequests.Where(x => institutionIds.Contains(x.InstitutionId)).ExecuteDeleteAsync(cancellationToken);

        var tenantUserIds = _db.Users
            .Where(x => x.InstitutionId.HasValue && institutionIds.Contains(x.InstitutionId.Value))
            .Select(x => x.Id);
        await _db.RefreshTokens.Where(x => tenantUserIds.Contains(x.UserId)).ExecuteDeleteAsync(cancellationToken);

        await _db.Users
            .Where(x => x.InstitutionId.HasValue && institutionIds.Contains(x.InstitutionId.Value) &&
                (x.Role == UserRole.Admin || x.Role == UserRole.InstitutionAdmin))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.StudentProfileId, (Guid?)null)
                .SetProperty(x => x.ParentProfileId, (Guid?)null)
                .SetProperty(x => x.TeacherProfileId, (Guid?)null), cancellationToken);

        await _db.Users.Where(x => x.InstitutionId.HasValue && institutionIds.Contains(x.InstitutionId.Value) &&
            x.Role != UserRole.Admin && x.Role != UserRole.InstitutionAdmin).ExecuteDeleteAsync(cancellationToken);
        await _db.Parents.Where(x => institutionIds.Contains(x.InstitutionId)).ExecuteDeleteAsync(cancellationToken);
        await _db.Teachers.Where(x => institutionIds.Contains(x.InstitutionId)).ExecuteDeleteAsync(cancellationToken);
        await _db.Students.Where(x => institutionIds.Contains(x.InstitutionId)).ExecuteDeleteAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return Ok(new
        {
            message = request.InstitutionId.HasValue ? "تمت إعادة ضبط بيانات المؤسسة بنجاح." : "تمت إعادة ضبط بيانات جميع المؤسسات بنجاح.",
            institutionsReset = institutionIds.Length,
            deletedUsers,
            deletedStudents,
            deletedExams,
            deletedAttempts
        });
    }

    private Guid CurrentSuperAdminId()
        => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw new UnauthorizedAccessException("تعذر تحديد حساب المشرف العام.");

    private static void RemoveExpiredChallenges()
    {
        foreach (var item in ResetChallenges.Where(x => x.Value.ExpiresAtUtc < DateTime.UtcNow))
            ResetChallenges.TryRemove(item.Key, out _);
    }

    private sealed record ResetChallenge(Guid UserId, Guid? InstitutionId, string VerificationCode, DateTime ExpiresAtUtc);
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

public sealed class CreateDataResetChallengeRequest
{
    public Guid? InstitutionId { get; set; }
}

public sealed class ResetDataRequest
{
    public Guid? InstitutionId { get; set; }
    public Guid ChallengeId { get; set; }
    public string VerificationCode { get; set; } = string.Empty;
}
