using Microsoft.EntityFrameworkCore;
using QuizSystem.Application.Contracts.Auth;
using QuizSystem.Application.Contracts.Users;
using QuizSystem.Application.DTOs;
using QuizSystem.Domain.Entities;
using QuizSystem.Domain.Enums;
using QuizSystem.Infrastructure.Persistence;

namespace QuizSystem.Infrastructure.Services.Users;

public class UserManagementService : IUserManagementService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public UserManagementService(AppDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<List<UserListItemDto>> GetUsersAsync(Guid? institutionId, bool isSuperAdmin, CancellationToken cancellationToken = default)
    {
        var query = ApplyScope(_db.Users, institutionId, isSuperAdmin);

        var users = await query
            .Include(x => x.Institution)
            .Include(x => x.StudentProfile)
            .Include(x => x.ParentProfile)
            .Include(x => x.TeacherProfile)
            .OrderBy(x => x.UserName)
            .ToListAsync(cancellationToken);

        return users.Select(MapUser).ToList();
    }

    public async Task<UserListItemDto> CreateUserAsync(Guid? institutionId, bool isSuperAdmin, CreateUserManagementRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.UserName))
            throw new InvalidOperationException("UserName is required");

        if (string.IsNullOrWhiteSpace(request.Password))
            throw new InvalidOperationException("Password is required");

        if (await _db.Users.AnyAsync(x => x.UserName == request.UserName, cancellationToken))
            throw new InvalidOperationException("Username already exists");

        var role = ParseRole(request.Role);
        if (role == UserRole.SuperAdmin && !isSuperAdmin)
            throw new UnauthorizedAccessException("Only SuperAdmin can create SuperAdmin users.");

        var userInstitutionId = role == UserRole.SuperAdmin
            ? (Guid?)null
            : ResolveTenantInstitution(institutionId, isSuperAdmin);

        ValidateRoleLinks(role, request.StudentProfileId, request.ParentProfileId, request.TeacherProfileId);
        await ValidateTeacherAsync(userInstitutionId, isSuperAdmin, request.TeacherProfileId, cancellationToken);

        if (request.StudentProfileId.HasValue)
        {
            var studentExists = await _db.Students.AnyAsync(
                x => x.Id == request.StudentProfileId.Value &&
                     (isSuperAdmin || x.InstitutionId == userInstitutionId),
                cancellationToken);
            if (!studentExists)
                throw new InvalidOperationException("Student profile not found");
        }

        if (request.ParentProfileId.HasValue)
        {
            var parentExists = await _db.Parents.AnyAsync(
                x => x.Id == request.ParentProfileId.Value &&
                     (isSuperAdmin || x.InstitutionId == userInstitutionId),
                cancellationToken);
            if (!parentExists)
                throw new InvalidOperationException("Parent profile not found");
        }

        var user = new AppUser
        {
            InstitutionId = userInstitutionId,
            UserName = request.UserName,
            PasswordHash = _passwordHasher.Hash(request.Password),
            MustChangePassword = true,
            Role = role,
            IsActive = true,
            StudentProfileId = request.StudentProfileId,
            ParentProfileId = request.ParentProfileId
            ,TeacherProfileId = request.TeacherProfileId
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        await _db.Entry(user).Reference(x => x.StudentProfile).LoadAsync(cancellationToken);
        await _db.Entry(user).Reference(x => x.ParentProfile).LoadAsync(cancellationToken);
        await _db.Entry(user).Reference(x => x.TeacherProfile).LoadAsync(cancellationToken);

        return MapUser(user);
    }

    public async Task<UserListItemDto> UpdateUserAsync(Guid? institutionId, bool isSuperAdmin, Guid userId, UpdateUserManagementRequest request, CancellationToken cancellationToken = default)
    {
        var user = await ApplyScope(_db.Users, institutionId, isSuperAdmin)
            .Include(x => x.Institution)
            .Include(x => x.StudentProfile)
            .Include(x => x.ParentProfile)
            .Include(x => x.TeacherProfile)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("User not found");

        if (string.IsNullOrWhiteSpace(request.UserName))
            throw new InvalidOperationException("UserName is required");

        var role = ParseRole(request.Role);
        if (role == UserRole.SuperAdmin && !isSuperAdmin)
            throw new UnauthorizedAccessException("Only SuperAdmin can assign SuperAdmin role.");

        var userInstitutionId = role == UserRole.SuperAdmin
            ? (Guid?)null
            : ResolveTenantInstitution(institutionId, isSuperAdmin);

        ValidateRoleLinks(role, request.StudentProfileId, request.ParentProfileId, request.TeacherProfileId);
        await ValidateTeacherAsync(userInstitutionId, isSuperAdmin, request.TeacherProfileId, cancellationToken);

        var duplicate = await _db.Users.AnyAsync(x => x.UserName == request.UserName && x.Id != userId, cancellationToken);
        if (duplicate)
            throw new InvalidOperationException("Username already exists");

        if (request.StudentProfileId.HasValue)
        {
            var studentExists = await _db.Students.AnyAsync(
                x => x.Id == request.StudentProfileId.Value &&
                     (isSuperAdmin || x.InstitutionId == userInstitutionId),
                cancellationToken);
            if (!studentExists)
                throw new InvalidOperationException("Student profile not found");
        }

        if (request.ParentProfileId.HasValue)
        {
            var parentExists = await _db.Parents.AnyAsync(
                x => x.Id == request.ParentProfileId.Value &&
                     (isSuperAdmin || x.InstitutionId == userInstitutionId),
                cancellationToken);
            if (!parentExists)
                throw new InvalidOperationException("Parent profile not found");
        }

        user.InstitutionId = userInstitutionId;
        user.UserName = request.UserName;
        user.Role = role;
        user.IsActive = request.IsActive;
        user.StudentProfileId = request.StudentProfileId;
        user.ParentProfileId = request.ParentProfileId;
        user.TeacherProfileId = request.TeacherProfileId;

        await _db.SaveChangesAsync(cancellationToken);

        await _db.Entry(user).Reference(x => x.Institution).LoadAsync(cancellationToken);
        await _db.Entry(user).Reference(x => x.StudentProfile).LoadAsync(cancellationToken);
        await _db.Entry(user).Reference(x => x.ParentProfile).LoadAsync(cancellationToken);
        await _db.Entry(user).Reference(x => x.TeacherProfile).LoadAsync(cancellationToken);

        return MapUser(user);
    }

    public async Task ToggleUserStatusAsync(Guid? institutionId, bool isSuperAdmin, Guid userId, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await ApplyScope(_db.Users, institutionId, isSuperAdmin)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("User not found");

        user.IsActive = isActive;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteUserAsync(Guid? institutionId, bool isSuperAdmin, Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await ApplyScope(_db.Users, institutionId, isSuperAdmin)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("User not found");

        _db.Users.Remove(user);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static UserRole ParseRole(string role)
    {
        if (Enum.TryParse<UserRole>(role, true, out var parsed))
            return parsed;

        throw new InvalidOperationException("Invalid role");
    }

    private static void ValidateRoleLinks(UserRole role, Guid? studentProfileId, Guid? parentProfileId, Guid? teacherProfileId)
    {
        if (role == UserRole.Student && !studentProfileId.HasValue)
            throw new InvalidOperationException("Student user must be linked to StudentProfileId");

        if (role == UserRole.Parent && !parentProfileId.HasValue)
            throw new InvalidOperationException("Parent user must be linked to ParentProfileId");

        if (role != UserRole.Student && studentProfileId.HasValue)
            throw new InvalidOperationException("StudentProfileId allowed only for Student role");

        if (role != UserRole.Parent && parentProfileId.HasValue)
            throw new InvalidOperationException("ParentProfileId allowed only for Parent role");

        if (role == UserRole.CourseSupervisor && !teacherProfileId.HasValue)
            throw new InvalidOperationException("يجب ربط مشرف المقرر بملف معلم");
        if (role != UserRole.CourseSupervisor && role != UserRole.Teacher && teacherProfileId.HasValue)
            throw new InvalidOperationException("ملف المعلم مسموح فقط للمعلم أو مشرف المقرر");
    }

    private async Task ValidateTeacherAsync(Guid? institutionId, bool isSuperAdmin, Guid? teacherProfileId, CancellationToken ct)
    {
        if (!teacherProfileId.HasValue) return;
        var exists = await _db.TeacherProfiles.AnyAsync(x => x.Id == teacherProfileId && (isSuperAdmin || x.InstitutionId == institutionId), ct);
        if (!exists) throw new InvalidOperationException("ملف المعلم غير موجود في المؤسسة الحالية");
    }

    public async Task ResetPasswordAsync(Guid? institutionId, bool isSuperAdmin, Guid userId, AdminResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8 || !request.NewPassword.Any(char.IsLetter) || !request.NewPassword.Any(char.IsDigit))
            throw new InvalidOperationException("Password must be at least 8 characters and contain letters and numbers");
        var user = await ApplyScope(_db.Users, institutionId, isSuperAdmin)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("User not found");
        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.MustChangePassword = true;
        user.PasswordResetTokenHash = null;
        user.PasswordResetTokenExpiresAtUtc = null;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<AppUser> ApplyScope(IQueryable<AppUser> query, Guid? institutionId, bool isSuperAdmin)
    {
        if (isSuperAdmin)
            return query;

        var tenantId = ResolveTenantInstitution(institutionId, isSuperAdmin);
        return query.Where(x => x.InstitutionId == tenantId);
    }

    private static Guid ResolveTenantInstitution(Guid? institutionId, bool isSuperAdmin)
    {
        if (institutionId is { } value && value != Guid.Empty)
            return value;

        if (isSuperAdmin)
            throw new InvalidOperationException("InstitutionId is required for tenant users.");

        throw new UnauthorizedAccessException("Current user is not linked to an institution.");
    }

    private static UserListItemDto MapUser(AppUser user)
    {
        return new UserListItemDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Role = user.Role.ToString(),
            IsActive = user.IsActive,
            Email = user.Email,
            MustChangePassword = user.MustChangePassword,
            InstitutionId = user.InstitutionId,
            InstitutionName = user.Institution?.Name,
            StudentProfileId = user.StudentProfileId,
            StudentName = user.StudentProfile?.FullName,
            ParentProfileId = user.ParentProfileId,
            ParentName = user.ParentProfile?.FullName
            ,TeacherProfileId = user.TeacherProfileId
            ,TeacherName = user.TeacherProfile?.FullName
        };
    }
}
