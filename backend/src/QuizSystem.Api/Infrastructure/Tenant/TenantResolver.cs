using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using QuizSystem.Infrastructure.Persistence;

namespace QuizSystem.Api.Infrastructure.Tenant;

public static class TenantResolver
{
    public static Guid? GetCurrentUserId(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? user.FindFirstValue("sub")
                  ?? user.FindFirstValue("userId");

        return Guid.TryParse(raw, out var userId) ? userId : null;
    }

    public static async Task<Guid?> GetCurrentInstitutionIdAsync(
        AppDbContext db,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var claimValue =
            user.FindFirstValue("institutionId") ??
            user.FindFirstValue("InstitutionId") ??
            user.FindFirstValue("schoolId") ??
            user.FindFirstValue("SchoolId");

        if (Guid.TryParse(claimValue, out var claimInstitutionId) && claimInstitutionId != Guid.Empty)
            return claimInstitutionId;

        var userId = GetCurrentUserId(user);
        if (userId is null) return null;

        return await db.Users
            .AsNoTracking()
            .Where(x => x.Id == userId.Value)
            .Select(x => x.InstitutionId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public static bool IsSuperAdmin(ClaimsPrincipal user)
        => user.IsInRole("SuperAdmin") || user.IsInRole("superadmin");

    public static async Task<Guid> RequireCurrentInstitutionIdAsync(AppDbContext db, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        var institutionId = await GetCurrentInstitutionIdAsync(db, user, cancellationToken);
        if (institutionId is null || institutionId.Value == Guid.Empty) throw new UnauthorizedAccessException("Current user is not linked to an institution.");
        return institutionId.Value;
    }
}
