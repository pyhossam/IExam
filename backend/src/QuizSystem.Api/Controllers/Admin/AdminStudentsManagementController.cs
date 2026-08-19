using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizSystem.Api.Infrastructure.Tenant;
using QuizSystem.Application.Contracts.Auth;
using QuizSystem.Domain.Entities;
using QuizSystem.Infrastructure.Persistence;

namespace QuizSystem.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/students")]
[Authorize(Policy = "AdminOnly")]
public class AdminStudentsManagementController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public AdminStudentsManagementController(AppDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    [HttpGet]
    public async Task<IActionResult> GetStudents(CancellationToken cancellationToken)
    {
        var institutionId = await TenantResolver.RequireCurrentInstitutionIdAsync(_db, User, cancellationToken);

        var users = await _db.Set<AppUser>()
            .AsNoTracking()
            .Where(x => x.InstitutionId == institutionId)
            .Where(x => x.StudentProfileId != null)
            .Select(x => new
            {
                x.Id,
                x.StudentProfileId,
                x.UserName,
                x.IsActive,
                x.InstitutionId,
                InstitutionName = x.Institution != null ? x.Institution.Name : null
            })
            .ToListAsync(cancellationToken);

        var userMap = users
            .Where(x => x.StudentProfileId != null)
            .GroupBy(x => x.StudentProfileId!.Value)
            .ToDictionary(x => x.Key, x => x.First());

        var parentLinks = await _db.Set<ParentStudentLink>()
            .AsNoTracking()
            .Where(x => x.InstitutionId == institutionId)
            .Select(x => new
            {
                x.StudentProfileId,
                x.ParentProfileId,
                ParentName = x.ParentProfile.FullName
            })
            .ToListAsync(cancellationToken);

        var parentMap = parentLinks
            .GroupBy(x => x.StudentProfileId)
            .ToDictionary(x => x.Key, x => x.First());

        var students = await _db.Set<StudentProfile>()
            .AsNoTracking()
            .Where(x => x.InstitutionId == institutionId)
            .OrderBy(x => x.FullName)
            .Select(x => new
            {
                x.Id,
                x.FullName,
                x.StudentCode,
                x.Grade,
                x.IsActive
            })
            .ToListAsync(cancellationToken);

        var result = students.Select(x =>
        {
            userMap.TryGetValue(x.Id, out var user);
            parentMap.TryGetValue(x.Id, out var parent);

            return new
            {
                id = x.Id,
                fullName = x.FullName,
                name = x.FullName,
                studentCode = x.StudentCode,
                code = x.StudentCode,
                grade = x.Grade,
                userName = user?.UserName,
                userId = user?.Id,
                institutionId = user?.InstitutionId,
                institutionName = user?.InstitutionName,
                parentProfileId = parent?.ParentProfileId,
                parentName = parent?.ParentName,
                isActive = user?.IsActive ?? x.IsActive
            };
        });

        return Ok(result);
    }

    [HttpPut("{studentId:guid}")]
    public async Task<IActionResult> UpdateStudent(
        Guid studentId,
        [FromBody] UpdateStudentManagementRequest request,
        CancellationToken cancellationToken)
    {
        var institutionId = await TenantResolver.RequireCurrentInstitutionIdAsync(_db, User, cancellationToken);

        var student = await _db.Set<StudentProfile>()
            .FirstOrDefaultAsync(x => x.Id == studentId && x.InstitutionId == institutionId, cancellationToken);

        if (student is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Student not found",
                Detail = "الطالب غير موجود",
                Status = StatusCodes.Status404NotFound
            });
        }

        student.FullName = request.FullName?.Trim() ?? student.FullName;
        student.StudentCode = request.StudentCode?.Trim() ?? student.StudentCode;
        student.Grade = request.Grade?.Trim() ?? student.Grade;
        student.IsActive = request.IsActive;

        var user = await _db.Set<AppUser>()
            .FirstOrDefaultAsync(x => x.StudentProfileId == studentId && x.InstitutionId == institutionId, cancellationToken);

        if (user is not null)
        {
            if (!string.IsNullOrWhiteSpace(request.UserName))
            {
                var userName = request.UserName.Trim();
                var userNameExists = await _db.Set<AppUser>()
                    .AnyAsync(x => x.Id != user.Id && x.UserName == userName, cancellationToken);

                if (userNameExists)
                {
                    return BadRequest(new ProblemDetails
                    {
                        Title = "Username already exists",
                        Detail = "اسم المستخدم مستخدم بالفعل",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                user.UserName = userName;
            }

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                user.PasswordHash = _passwordHasher.Hash(request.Password);
            }

            user.IsActive = request.IsActive;
        }
        else if (!string.IsNullOrWhiteSpace(request.UserName) && !string.IsNullOrWhiteSpace(request.Password))
        {
            var userName = request.UserName.Trim();
            var userNameExists = await _db.Set<AppUser>()
                .AnyAsync(x => x.UserName == userName, cancellationToken);

            if (userNameExists)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Username already exists",
                    Detail = "اسم المستخدم مستخدم بالفعل",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            _db.Set<AppUser>().Add(new AppUser
            {
                Id = Guid.NewGuid(),
                InstitutionId = institutionId,
                UserName = userName,
                PasswordHash = _passwordHasher.Hash(request.Password),
                Role = Domain.Enums.UserRole.Student,
                StudentProfileId = studentId,
                IsActive = request.IsActive
            });
        }

        var oldLinks = await _db.Set<ParentStudentLink>()
            .Where(x => x.StudentProfileId == studentId && x.InstitutionId == institutionId)
            .ToListAsync(cancellationToken);

        _db.Set<ParentStudentLink>().RemoveRange(oldLinks);

        if (request.ParentProfileId.HasValue)
        {
            var parentExists = await _db.Set<ParentProfile>()
                .AnyAsync(x => x.Id == request.ParentProfileId.Value && x.InstitutionId == institutionId, cancellationToken);

            if (!parentExists)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Parent not found",
                    Detail = "ولي الأمر غير موجود داخل نفس المؤسسة",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            _db.Set<ParentStudentLink>().Add(new ParentStudentLink
            {
                Id = Guid.NewGuid(),
                InstitutionId = institutionId,
                StudentProfileId = studentId,
                ParentProfileId = request.ParentProfileId.Value
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Student updated successfully" });
    }

    [HttpPatch("{studentId:guid}/status")]
    public async Task<IActionResult> UpdateStudentStatus(
        Guid studentId,
        [FromQuery] bool isActive,
        CancellationToken cancellationToken)
    {
        var institutionId = await TenantResolver.RequireCurrentInstitutionIdAsync(_db, User, cancellationToken);

        var student = await _db.Set<StudentProfile>()
            .FirstOrDefaultAsync(x => x.Id == studentId && x.InstitutionId == institutionId, cancellationToken);

        if (student is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Student not found",
                Detail = "الطالب غير موجود",
                Status = StatusCodes.Status404NotFound
            });
        }

        student.IsActive = isActive;

        var user = await _db.Set<AppUser>()
            .FirstOrDefaultAsync(x => x.StudentProfileId == studentId && x.InstitutionId == institutionId, cancellationToken);

        if (user is not null)
            user.IsActive = isActive;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            id = student.Id,
            isActive,
            message = isActive ? "Student activated" : "Student deactivated"
        });
    }

    [HttpDelete("{studentId:guid}")]
    public async Task<IActionResult> DeleteStudent(Guid studentId, CancellationToken cancellationToken)
    {
        var institutionId = await TenantResolver.RequireCurrentInstitutionIdAsync(_db, User, cancellationToken);

        var student = await _db.Set<StudentProfile>()
            .FirstOrDefaultAsync(x => x.Id == studentId && x.InstitutionId == institutionId, cancellationToken);

        if (student is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Student not found",
                Detail = "الطالب غير موجود",
                Status = StatusCodes.Status404NotFound
            });
        }

        var hasAttempts = await _db.Set<ExamAttempt>()
            .AnyAsync(x => x.StudentProfileId == studentId && x.InstitutionId == institutionId, cancellationToken);

        var hasRegistrations = await _db.Set<ExamRegistration>()
            .AnyAsync(x => x.StudentProfileId == studentId && x.InstitutionId == institutionId && x.IsActive, cancellationToken);

        if (hasAttempts || hasRegistrations)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Cannot delete student",
                Detail = "لا يمكن حذف الطالب لوجود محاولات أو تسجيلات مرتبطة به. يمكن تعطيله بدل الحذف.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var links = await _db.Set<ParentStudentLink>()
            .Where(x => x.StudentProfileId == studentId && x.InstitutionId == institutionId)
            .ToListAsync(cancellationToken);

        var users = await _db.Set<AppUser>()
            .Where(x => x.StudentProfileId == studentId && x.InstitutionId == institutionId)
            .ToListAsync(cancellationToken);

        _db.Set<ParentStudentLink>().RemoveRange(links);
        _db.Set<AppUser>().RemoveRange(users);
        _db.Set<StudentProfile>().Remove(student);

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Student deleted successfully" });
    }
}

public class UpdateStudentManagementRequest
{
    public string? FullName { get; set; }
    public string? StudentCode { get; set; }
    public string? Grade { get; set; }
    public string? Branch { get; set; }
    public string? NationalId { get; set; }
    public string? Mobile { get; set; }
    public string? Nationality { get; set; }
    public string? ImagePath { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public Guid? ParentProfileId { get; set; }
    public bool IsActive { get; set; } = true;
}
