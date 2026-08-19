namespace QuizSystem.Application.Contracts.Auth;

public interface IEmailSender
{
    Task SendPasswordResetAsync(string email, string resetUrl, CancellationToken cancellationToken = default);
    Task SendEmailVerificationAsync(string email, string verificationUrl, CancellationToken cancellationToken = default);
    Task SendNotificationAsync(string email, string subject, string body, CancellationToken cancellationToken = default);
}
