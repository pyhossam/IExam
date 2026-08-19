using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QuizSystem.Application.Contracts.Attempts;
using QuizSystem.Application.Contracts.Auth;
using QuizSystem.Application.Contracts.Exams;
using QuizSystem.Application.Contracts.Imports;
using QuizSystem.Application.Contracts.Portals;
using QuizSystem.Application.Contracts.Reports;
using QuizSystem.Application.DTOs;
using QuizSystem.Infrastructure.Persistence;
using System.Security.Cryptography;
using System.Text;

namespace QuizSystem.Infrastructure.Services.Auth;
public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;

    public AuthService(AppDbContext db, IPasswordHasher passwordHasher, IJwtTokenService jwtTokenService, IEmailSender emailSender, IConfiguration configuration)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _emailSender = emailSender;
        _configuration = configuration;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.UserName == request.UserName && x.IsActive, cancellationToken);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid username or password");

        var (token, expiresAtUtc) = _jwtTokenService.GenerateToken(user);

        return new AuthResponse(
            token,
            user.UserName,
            user.Role.ToString(),
            expiresAtUtc,
            user.MustChangePassword || string.IsNullOrWhiteSpace(user.Email),
            user.Email
        );
    }

    public async Task CompleteFirstLoginAsync(Guid userId, CompleteFirstLoginRequest request, CancellationToken cancellationToken = default)
    {
        ValidateEmail(request.Email);
        ValidatePassword(request.NewPassword);
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId && x.IsActive, cancellationToken)
            ?? throw new UnauthorizedAccessException("User not found");
        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect");
        if (await _db.Users.AnyAsync(x => x.Id != userId && x.Email == NormalizeEmail(request.Email), cancellationToken))
            throw new InvalidOperationException("Email is already used by another account");

        user.Email = NormalizeEmail(request.Email);
        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.MustChangePassword = false;
        user.PasswordResetTokenHash = null;
        user.PasswordResetTokenExpiresAtUtc = null;
        var verificationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        user.EmailVerificationTokenHash = HashToken(verificationToken);
        user.EmailVerificationTokenExpiresAtUtc = DateTime.UtcNow.AddHours(24);
        user.EmailVerifiedAtUtc = null;
        await _db.SaveChangesAsync(cancellationToken);
        var baseUrl = (_configuration["App:FrontendBaseUrl"] ?? "http://localhost:5190").TrimEnd('/');
        await _emailSender.SendEmailVerificationAsync(user.Email, $"{baseUrl}/verify-email?token={Uri.EscapeDataString(verificationToken)}", cancellationToken);
    }

    public async Task RequestPasswordResetAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == email && x.IsActive, cancellationToken);
        if (user is null) return;

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        user.PasswordResetTokenHash = HashToken(token);
        user.PasswordResetTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(30);
        await _db.SaveChangesAsync(cancellationToken);

        var baseUrl = (_configuration["App:FrontendBaseUrl"] ?? "http://localhost:5190").TrimEnd('/');
        await _emailSender.SendPasswordResetAsync(user.Email!, $"{baseUrl}/reset-password?token={Uri.EscapeDataString(token)}", cancellationToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        ValidatePassword(request.NewPassword);
        var hash = HashToken(request.Token);
        var now = DateTime.UtcNow;
        var user = await _db.Users.FirstOrDefaultAsync(x => x.PasswordResetTokenHash == hash && x.PasswordResetTokenExpiresAtUtc > now && x.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Reset link is invalid or expired");

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.MustChangePassword = false;
        user.PasswordResetTokenHash = null;
        user.PasswordResetTokenExpiresAtUtc = null;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default)
    {
        var hash = HashToken(request.Token);
        var now = DateTime.UtcNow;
        var user = await _db.Users.FirstOrDefaultAsync(x => x.EmailVerificationTokenHash == hash && x.EmailVerificationTokenExpiresAtUtc > now && x.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Verification link is invalid or expired");
        user.EmailVerifiedAtUtc = now;
        user.EmailVerificationTokenHash = null;
        user.EmailVerificationTokenExpiresAtUtc = null;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
    private static void ValidateEmail(string email)
    {
        try { _ = new global::System.Net.Mail.MailAddress(email); }
        catch { throw new InvalidOperationException("A valid email address is required"); }
    }
    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8 || !password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            throw new InvalidOperationException("Password must be at least 8 characters and contain letters and numbers");
    }
    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
