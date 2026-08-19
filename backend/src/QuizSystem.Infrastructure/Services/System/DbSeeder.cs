using Microsoft.EntityFrameworkCore;
using QuizSystem.Application.Contracts.Attempts;
using QuizSystem.Application.Contracts.Auth;
using QuizSystem.Application.Contracts.Exams;
using QuizSystem.Application.Contracts.Imports;
using QuizSystem.Application.Contracts.Portals;
using QuizSystem.Application.Contracts.Reports;
using QuizSystem.Domain.Entities;
using QuizSystem.Domain.Enums;
using QuizSystem.Infrastructure.Persistence;

namespace QuizSystem.Infrastructure.Services.System;
public static class DbSeeder
{
    private const string DefaultInstitutionName = "Default Institution";

    public static async Task SeedAsync(AppDbContext db, IPasswordHasher passwordHasher)
    {
        await db.Database.MigrateAsync();

        var defaultInstitution = await db.Institutions
            .FirstOrDefaultAsync(x => x.Name == DefaultInstitutionName);

        if (defaultInstitution is null)
        {
            defaultInstitution = new Institution
            {
                Name = DefaultInstitutionName,
                Type = "Default",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            db.Institutions.Add(defaultInstitution);
            await db.SaveChangesAsync();
        }

        if (!await db.Users.AnyAsync(x => x.UserName == "admin"))
        {
            db.Users.Add(new AppUser
            {
                UserName = "admin",
                PasswordHash = passwordHasher.Hash("Admin@123"),
                Role = UserRole.Admin,
                IsActive = true,
                InstitutionId = defaultInstitution.Id
            });

            await db.SaveChangesAsync();
        }

        var usersWithoutInstitution = await db.Users
            .Where(x =>
                x.InstitutionId == null &&
                x.Role != UserRole.SuperAdmin)
            .ToListAsync();

        foreach (var user in usersWithoutInstitution)
            user.InstitutionId = defaultInstitution.Id;

        if (usersWithoutInstitution.Count > 0)
            await db.SaveChangesAsync();
    }
}
