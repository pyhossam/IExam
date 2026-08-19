using System.Net;
using System.Net.Mail;
using QuizSystem.Application.Contracts.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace QuizSystem.Infrastructure.Services.Auth;

public sealed class SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendPasswordResetAsync(string email, string resetUrl, CancellationToken cancellationToken = default)
        => await SendAsync(email, "إعادة تعيين كلمة مرور IExam", $"استخدم الرابط التالي لإعادة تعيين كلمة المرور. تنتهي صلاحية الرابط خلال 30 دقيقة:\n\n{resetUrl}", cancellationToken);

    public async Task SendEmailVerificationAsync(string email, string verificationUrl, CancellationToken cancellationToken = default)
        => await SendAsync(email, "تأكيد البريد الإلكتروني في IExam", $"اضغط الرابط التالي لتأكيد بريدك الإلكتروني. تنتهي صلاحية الرابط خلال 24 ساعة:\n\n{verificationUrl}", cancellationToken);

    public Task SendNotificationAsync(string email, string subject, string body, CancellationToken cancellationToken = default)
        => SendAsync(email, subject, body, cancellationToken);

    private async Task SendAsync(string email, string subject, string body, CancellationToken cancellationToken)
    {
        var host = configuration["Smtp:Host"];
        var from = configuration["Smtp:From"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
            throw new InvalidOperationException("Email service is not configured.");

        var port = int.TryParse(configuration["Smtp:Port"], out var value) ? value : 587;
        var enableSsl = !bool.TryParse(configuration["Smtp:EnableSsl"], out var ssl) || ssl;
        using var client = new SmtpClient(host, port) { EnableSsl = enableSsl };
        var userName = configuration["Smtp:UserName"];
        var password = configuration["Smtp:Password"];
        if (!string.IsNullOrWhiteSpace(userName))
            client.Credentials = new NetworkCredential(userName, password);

        using var message = new MailMessage(from, email)
        {
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };

        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message, cancellationToken);
        logger.LogInformation("Password reset email sent to configured user address.");
    }
}
