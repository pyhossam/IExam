using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;

namespace QuizSystem.Api.Infrastructure.Errors;

public sealed class ProfessionalExceptionMiddleware(RequestDelegate next, ILogger<ProfessionalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            var traceId = context.TraceIdentifier;
            var (status, title, detail) = Map(exception);

            if (status >= 500)
                logger.LogError(exception, "Unhandled request error. TraceId: {TraceId}", traceId);
            else
                logger.LogWarning("Request rejected: {Message}. TraceId: {TraceId}", exception.Message, traceId);

            if (context.Response.HasStarted) throw;
            context.Response.Clear();
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json; charset=utf-8";
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path,
                Extensions = { ["traceId"] = traceId }
            });
        }
    }

    private static (int Status, string Title, string Detail) Map(Exception exception) => exception switch
    {
        KeyNotFoundException => (404, "العنصر غير موجود", "تعذر العثور على البيانات المطلوبة، ربما تم حذفها أو لم تعد متاحة."),
        UnauthorizedAccessException => (403, "غير مسموح", "ليس لديك صلاحية لتنفيذ هذه العملية."),
        SmtpException => (503, "تعذر إرسال البريد", "تعذر إرسال الرسالة حاليًا. تحقق من إعدادات البريد أو حاول مرة أخرى لاحقًا."),
        DbUpdateException when IsDuplicate(exception) => (400, "بيانات مكررة", "القيمة المدخلة مستخدمة بالفعل. راجع البيانات وحاول بقيمة مختلفة."),
        DbUpdateException or SqliteException => (500, "تعذر حفظ البيانات", "حدث خطأ أثناء حفظ البيانات. لم يتم تنفيذ العملية، حاول مرة أخرى."),
        ArgumentException => (400, "بيانات غير صالحة", FriendlyDetail(exception.Message)),
        InvalidOperationException => (400, "تعذر تنفيذ العملية", FriendlyDetail(exception.Message)),
        _ => (500, "حدث خطأ غير متوقع", "تعذر إكمال الطلب حاليًا. حاول مرة أخرى، وإذا استمرت المشكلة تواصل مع الدعم وأرسل رقم التتبع.")
    };

    private static string FriendlyDetail(string message)
    {
        var text = message.ToLowerInvariant();
        if (text.Contains("email is already used")) return "البريد الإلكتروني مستخدم في حساب آخر. استخدم بريدًا مختلفًا لكل حساب.";
        if (text.Contains("current password is incorrect")) return "كلمة المرور الحالية غير صحيحة.";
        if (text.Contains("password must")) return "يجب أن تتكون كلمة المرور من 8 أحرف على الأقل، وأن تحتوي على حروف وأرقام.";
        if (text.Contains("reset link is invalid or expired")) return "رابط إعادة تعيين كلمة المرور غير صالح أو انتهت صلاحيته. اطلب رابطًا جديدًا.";
        if (text.Contains("verification link is invalid or expired")) return "رابط تأكيد البريد غير صالح أو انتهت صلاحيته.";
        if (text.Contains("username already exists")) return "اسم المستخدم مستخدم بالفعل. اختر اسمًا مختلفًا.";
        if (text.Contains("exam code already exists")) return "كود الاختبار مستخدم بالفعل. اختر كودًا مختلفًا.";
        if (text.Contains("not found")) return "تعذر العثور على البيانات المطلوبة.";
        if (ContainsTechnicalDetails(text)) return "تعذر تنفيذ العملية بسبب بيانات غير صالحة. راجع الحقول وحاول مرة أخرى.";
        return message;
    }

    private static bool IsDuplicate(Exception exception)
    {
        var text = exception.ToString().ToLowerInvariant();
        return text.Contains("unique constraint") || text.Contains("duplicate") || text.Contains("sqlite error 19");
    }

    private static bool ContainsTechnicalDetails(string text) =>
        text.Contains("system.") || text.Contains("microsoft.") || text.Contains("sqlite") ||
        text.Contains("sql ") || text.Contains("stack trace") || text.Contains("constraint");
}

public static class ProfessionalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseProfessionalExceptionHandling(this IApplicationBuilder app)
        => app.UseMiddleware<ProfessionalExceptionMiddleware>();
}
