namespace QuizSystem.Application.DTOs;
public record LoginRequest(string UserName, string Password);

public record AuthResponse(
    string AccessToken,
    string UserName,
    string Role,
    DateTime ExpiresAtUtc,
    bool RequiresAccountSetup,
    string? Email
);

public sealed record CompleteFirstLoginRequest(string Email, string CurrentPassword, string NewPassword);
public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string Token, string NewPassword);
public sealed record VerifyEmailRequest(string Token);
