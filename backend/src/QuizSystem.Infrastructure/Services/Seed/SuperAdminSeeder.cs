using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QuizSystem.Application.Contracts.Auth;
using QuizSystem.Domain.Entities;
using QuizSystem.Domain.Enums;
using QuizSystem.Infrastructure.Persistence;

namespace QuizSystem.Infrastructure.Services.Seed;

public class SuperAdminSeeder
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;

    public SuperAdminSeeder(
        AppDbContext db,
        IPasswordHasher passwordHasher,
        IConfiguration configuration)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }

    public async Task SeedAsync()
    {
        var username = _configuration["SuperAdmin:UserName"] ?? "superadmin";
        var password = _configuration["SuperAdmin:Password"] ?? "Super@123456";

        var exists = await _db.Users.AnyAsync(x => x.Role == UserRole.SuperAdmin);
        if (exists)
            return;

        var user = new AppUser
        {
            UserName = username,
            Role = UserRole.SuperAdmin,
            IsActive = true
        };

        user.PasswordHash = _passwordHasher.Hash(password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
    }
}
