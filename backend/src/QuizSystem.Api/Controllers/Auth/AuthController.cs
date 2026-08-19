using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using QuizSystem.Application.Contracts.Auth;
using QuizSystem.Application.DTOs;
using QuizSystem.Application.DTOs.Auth;

namespace QuizSystem.Api.Controllers.Auth;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [Authorize]
    [HttpPost("complete-first-login")]
    public async Task<IActionResult> CompleteFirstLogin([FromBody] CompleteFirstLoginRequest request, CancellationToken cancellationToken)
    {
        var rawUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(rawUserId, out var userId)) return Unauthorized();
        try
        {
            await _authService.CompleteFirstLoginAsync(userId, request, cancellationToken);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            var detail = ex.Message.Contains("Current password", StringComparison.OrdinalIgnoreCase)
                ? "كلمة المرور الحالية غير صحيحة."
                : "تعذر التحقق من الحساب.";
            return BadRequest(new ProblemDetails { Title = "تعذر تحديث الحساب", Detail = detail, Status = StatusCodes.Status400BadRequest });
        }
        catch (InvalidOperationException ex)
        {
            var detail = ex.Message.Contains("Email is already used", StringComparison.OrdinalIgnoreCase)
                ? "البريد الإلكتروني مستخدم في حساب آخر. استخدم بريدًا مختلفًا لكل حساب."
                : ex.Message;
            return BadRequest(new ProblemDetails { Title = "بيانات الحساب غير صالحة", Detail = detail, Status = StatusCodes.Status400BadRequest });
        }
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await _authService.RequestPasswordResetAsync(request, cancellationToken);
        return Ok(new { message = "If the email is registered, a reset link has been sent." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await _authService.ResetPasswordAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        await _authService.VerifyEmailAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.LoginAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("refresh")]
    public IActionResult Refresh([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Unauthorized();
        }

        return Ok(new
        {
            accessToken = "new-access-token",
            refreshToken = request.RefreshToken
        });
    }
}
