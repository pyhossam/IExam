using QuizSystem.Application.DTOs;

namespace QuizSystem.Application.Contracts.Auth;
public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task CompleteFirstLoginAsync(Guid userId, CompleteFirstLoginRequest request, CancellationToken cancellationToken = default);
    Task RequestPasswordResetAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
    Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default);
}
