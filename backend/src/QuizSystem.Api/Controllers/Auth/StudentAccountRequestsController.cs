using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizSystem.Application.Contracts.Auth;
using QuizSystem.Domain.Entities;
using QuizSystem.Domain.Enums;
using QuizSystem.Infrastructure.Persistence;

namespace QuizSystem.Api.Controllers.Auth;

[ApiController]
public class StudentAccountRequestsController(
    AppDbContext db,
    IPasswordHasher passwordHasher,
    IEmailSender emailSender,
    IConfiguration configuration) : ControllerBase
{
    private static readonly HashSet<string> Stages = ["Primary", "Intermediate", "Secondary", "University"];
    private static readonly HashSet<string> Genders = ["Male", "Female"];

    [AllowAnonymous]
    [HttpGet("api/public/institutions")]
    public async Task<IActionResult> Institutions(CancellationToken ct) => Ok(await db.Institutions.AsNoTracking()
        .Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new { x.Id, x.Name }).ToListAsync(ct));

    [AllowAnonymous]
    [HttpPost("api/public/student-account-requests")]
    public async Task<IActionResult> Submit([FromBody] SubmitStudentAccountRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);
        if (string.IsNullOrWhiteSpace(request.FullName)) return Validation("الاسم مطلوب.");
        if (!Genders.Contains(request.Gender)) return Validation("يرجى اختيار النوع.");
        if (!Stages.Contains(request.EducationStage)) return Validation("يرجى اختيار المرحلة الدراسية.");
        if (request.Password.Length < 8 || !request.Password.Any(char.IsLetter) || !request.Password.Any(char.IsDigit))
            return Validation("كلمة المرور يجب ألا تقل عن 8 أحرف وأن تحتوي على حروف وأرقام.");
        if (!await db.Institutions.AnyAsync(x => x.Id == request.InstitutionId && x.IsActive, ct)) return Validation("المؤسسة التعليمية غير متاحة.");
        if (await db.Users.AnyAsync(x => x.Email == email || x.UserName == email, ct) || await db.StudentAccountRequests.AnyAsync(x => x.Email == email, ct))
            return Conflict(new ProblemDetails { Title = "البريد مستخدم", Detail = "البريد الإلكتروني مسجل بالفعل ولا يمكن استخدامه مرة أخرى.", Status = 409 });

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var row = new StudentAccountRequest
        {
            InstitutionId = request.InstitutionId, FullName = request.FullName.Trim(), Email = email,
            Gender = request.Gender, EducationStage = request.EducationStage,
            PasswordHash = passwordHasher.Hash(request.Password), EmailVerificationTokenHash = HashToken(token),
            EmailVerificationTokenExpiresAtUtc = DateTime.UtcNow.AddHours(24)
        };
        db.StudentAccountRequests.Add(row);
        await db.SaveChangesAsync(ct);
        var baseUrl = (configuration["App:FrontendBaseUrl"] ?? "http://localhost:5190").TrimEnd('/');
        await emailSender.SendEmailVerificationAsync(email, $"{baseUrl}/verify-student-registration?token={Uri.EscapeDataString(token)}", ct);
        return Accepted(new { id = row.Id, message = "تم استلام البيانات. افتح رسالة التفعيل المرسلة إلى بريدك لإكمال تقديم الطلب." });
    }

    [AllowAnonymous]
    [HttpPost("api/public/student-account-requests/verify-email")]
    public async Task<IActionResult> Verify([FromBody] VerifyStudentRequest request, CancellationToken ct)
    {
        var hash = HashToken(request.Token);
        var row = await db.StudentAccountRequests.Include(x => x.Institution)
            .FirstOrDefaultAsync(x => x.EmailVerificationTokenHash == hash && x.EmailVerificationTokenExpiresAtUtc > DateTime.UtcNow, ct);
        if (row is null) return BadRequest(new ProblemDetails { Title = "رابط غير صالح", Detail = "رابط التفعيل غير صالح أو انتهت صلاحيته.", Status = 400 });
        row.EmailVerifiedAtUtc = DateTime.UtcNow; row.EmailVerificationTokenHash = null; row.EmailVerificationTokenExpiresAtUtc = null; row.Status = "Pending";
        await db.SaveChangesAsync(ct);
        await emailSender.SendNotificationAsync(row.Email, "تم تقديم طلب حساب الطالب", $"مرحباً {row.FullName}،\n\nتم تفعيل بريدك وتقديم طلب الانضمام إلى {row.Institution.Name} بنجاح. حالة الطلب الآن: تحت الإجراء. سيصلك بريد عند قبول الطلب أو رفضه.", ct);
        return Ok(new { message = "تم تفعيل البريد وتقديم الطلب بنجاح، والطلب الآن تحت الإجراء." });
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpGet("api/admin/student-account-requests")]
    public async Task<IActionResult> List([FromQuery] string status = "Pending", CancellationToken ct = default)
    {
        var institutionId = InstitutionId();
        return Ok(await db.StudentAccountRequests.AsNoTracking().Where(x => x.InstitutionId == institutionId && x.Status == status)
            .OrderBy(x => x.CreatedAtUtc).Select(x => new { x.Id, x.FullName, x.Email, x.Gender, x.EducationStage, x.Status, x.EmailVerifiedAtUtc, x.CreatedAtUtc }).ToListAsync(ct));
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost("api/admin/student-account-requests/{id:guid}/decision")]
    public async Task<IActionResult> Decide(Guid id, [FromBody] StudentRequestDecision request, CancellationToken ct)
    {
        var institutionId = InstitutionId();
        var row = await db.StudentAccountRequests.Include(x => x.Institution).FirstOrDefaultAsync(x => x.Id == id && x.InstitutionId == institutionId, ct);
        if (row is null) return NotFound();
        if (row.Status != "Pending" || row.EmailVerifiedAtUtc is null) return Validation("لا يمكن اتخاذ قرار على طلب غير مكتمل أو تمت معالجته سابقاً.");

        if (!request.Approve)
        {
            row.Status = "Rejected"; row.RejectionReason = string.IsNullOrWhiteSpace(request.Reason) ? "يرجى مراجعة إدارة المؤسسة." : request.Reason.Trim();
            row.DecidedAtUtc = DateTime.UtcNow; row.DecidedByUserId = CurrentUserId(); await db.SaveChangesAsync(ct);
            await emailSender.SendNotificationAsync(row.Email, "نتيجة طلب حساب الطالب", $"نأسف، لم تتم الموافقة على طلبك للانضمام إلى {row.Institution.Name}.\nالسبب: {row.RejectionReason}\nيرجى مراجعة إدارة المؤسسة.", ct);
            return Ok(new { status = row.Status });
        }

        if (await db.Users.AnyAsync(x => x.Email == row.Email || x.UserName == row.Email, ct)) return Conflict(new { message = "البريد الإلكتروني مستخدم بالفعل." });
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var student = new StudentProfile { InstitutionId = institutionId, FullName = row.FullName, StudentCode = await NextStudentCode(institutionId, ct), Grade = StageArabic(row.EducationStage), Gender = row.Gender, NationalId = $"SELF-{row.Id:N}", IsActive = true };
        db.Students.Add(student);
        db.Users.Add(new AppUser { UserName = row.Email, Email = row.Email, PasswordHash = row.PasswordHash, Role = UserRole.Student, InstitutionId = institutionId, StudentProfile = student, IsActive = true, MustChangePassword = false, EmailVerifiedAtUtc = row.EmailVerifiedAtUtc });
        row.Status = "Approved"; row.DecidedAtUtc = DateTime.UtcNow; row.DecidedByUserId = CurrentUserId();
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        var loginUrl = (configuration["App:FrontendBaseUrl"] ?? "http://localhost:5190").TrimEnd('/') + "/login";
        await emailSender.SendNotificationAsync(row.Email, "تم قبول طلب حساب الطالب", $"مرحباً {row.FullName}،\n\nتم قبول طلبك في {row.Institution.Name}. يمكنك تسجيل الدخول باستخدام بريدك الإلكتروني من الرابط:\n{loginUrl}", ct);
        return Ok(new { status = row.Status, studentId = student.Id });
    }

    private IActionResult Validation(string detail) => BadRequest(new ProblemDetails { Title = "بيانات غير مكتملة", Detail = detail, Status = 400 });
    private Guid InstitutionId() => Guid.TryParse(User.FindFirstValue("institutionId"), out var id) ? id : throw new UnauthorizedAccessException("Institution is required");
    private Guid CurrentUserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : throw new UnauthorizedAccessException();
    private static string NormalizeEmail(string email) { try { return new System.Net.Mail.MailAddress(email.Trim()).Address.ToLowerInvariant(); } catch { throw new InvalidOperationException("البريد الإلكتروني غير صالح."); } }
    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private async Task<string> NextStudentCode(Guid institutionId, CancellationToken ct) { for (var i=0;i<20;i++) { var code=$"STD-{RandomNumberGenerator.GetInt32(100000,999999)}"; if (!await db.Students.AnyAsync(x=>x.InstitutionId==institutionId&&x.StudentCode==code,ct)) return code; } throw new InvalidOperationException("تعذر إنشاء رقم طالب فريد."); }
    private static string StageArabic(string value) => value switch { "Primary" => "ابتدائي", "Intermediate" => "متوسط", "Secondary" => "ثانوي", "University" => "جامعي", _ => value };
}

public sealed record SubmitStudentAccountRequest(string FullName, string Email, string Gender, string EducationStage, Guid InstitutionId, string Password);
public sealed record VerifyStudentRequest(string Token);
public sealed record StudentRequestDecision(bool Approve, string? Reason);
