using System.Text.RegularExpressions;
using QuizSystem.Infrastructure.Services.Seed;
using System.Threading.RateLimiting;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using QuizSystem.Infrastructure.Persistence;
using QuizSystem.Application.Contracts.Auth;
using QuizSystem.Infrastructure.Services.Auth;
using QuizSystem.Application.Interfaces;
using QuizSystem.Application.Contracts.Attempts;
using QuizSystem.Application.Contracts.Exams;
using QuizSystem.Infrastructure.Services.Admin;
using QuizSystem.Infrastructure.Services.Attempts;
using QuizSystem.Infrastructure.Services.Exams;
using QuizSystem.Application.Contracts.Imports;
using QuizSystem.Infrastructure.Services.Imports;
using QuizSystem.Application.Contracts.Portals;
using QuizSystem.Infrastructure.Services.Portals;
using QuizSystem.Application.Contracts.Reports;
using QuizSystem.Infrastructure.Services.Reports;
using QuizSystem.Infrastructure.Services.System;
using QuizSystem.Application.Contracts.Users;
using QuizSystem.Infrastructure.Services.Users;
using QuizSystem.Application.Contracts.Lookups;
using QuizSystem.Application.Contracts.SchoolManagement;
using QuizSystem.Infrastructure.Services.Lookups;
using QuizSystem.Infrastructure.Services.SchoolManagement;
using QuizSystem.Api.Infrastructure.Errors;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("LoginPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy("ApiPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey:
                context.User.Identity?.Name ??
                context.Connection.RemoteIpAddress?.ToString() ??
                "anonymous",

            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.RejectionStatusCode = 429;
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "QuizSystem API",
        Version = "v1",
        Description = "Quiz System API using .NET 9"
    });

    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Description = "Enter JWT token only",
        Reference = new OpenApiReference
        {
            Id = "Bearer",
            Type = ReferenceType.SecurityScheme
        }
    };

    options.AddSecurityDefinition("Bearer", jwtSecurityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtSecurityScheme, Array.Empty<string>() }
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IStudentExamService, StudentExamService>();
builder.Services.AddScoped<IExamManagementService, ExamManagementService>();
builder.Services.AddHttpClient<IAiQuestionGenerator, OpenAiQuestionGenerator>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(5);
});
builder.Services.AddScoped<IDashboardAnalyticsService, DashboardAnalyticsService>();
builder.Services.AddScoped<IAttemptManagementService, AttemptManagementService>();
builder.Services.AddScoped<IExamPdfService, ExamPdfService>();
builder.Services.AddScoped<IExcelImportService, ExcelImportService>();
builder.Services.AddScoped<IPortalService, PortalService>();
builder.Services.AddScoped<IReportPdfService, ReportPdfService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<ILookupService, LookupService>();
builder.Services.AddScoped<ISchoolManagementService, SchoolManagementService>();
var jwt = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwt["Key"]!);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdminOnly", p => p.RequireRole("SuperAdmin"));
    options.AddPolicy("InstitutionAdminOnly", p => p.RequireRole("InstitutionAdmin", "SchoolAdmin"));
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin", "InstitutionAdmin", "SchoolAdmin"));
    options.AddPolicy("AdminOrSupervisor", p => p.RequireRole("Admin", "InstitutionAdmin", "SchoolAdmin", "ExamSupervisor", "CourseSupervisor"));
    options.AddPolicy("SuperOrInstitutionAdmin", p => p.RequireRole("SuperAdmin", "Admin", "InstitutionAdmin", "SchoolAdmin"));
    options.AddPolicy("TeacherOnly", p => p.RequireRole("Teacher"));
    options.AddPolicy("TeacherOrExamManager", p => p.RequireRole("Teacher", "ExamSupervisor", "InstitutionAdmin", "SchoolAdmin", "Admin"));
    options.AddPolicy("StudentOnly", p => p.RequireRole("Student"));
    options.AddPolicy("ParentOnly", p => p.RequireRole("Parent"));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});
QuestPDF.Settings.License = LicenseType.Community;
QuestPdfFontRegistrar.RegisterFonts();
builder.Services.AddScoped<SuperAdminSeeder>();

var app = builder.Build();

app.UseProfessionalExceptionHandling();
app.UseFriendlyDuplicateErrors();
app.UseFriendlyStudentErrors();
app.UseStaticFiles();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await DbSeeder.SeedAsync(db, hasher);
}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "QuizSystem API v1");
    options.RoutePrefix = "swagger";
});

app.UseCors("frontend");
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<SuperAdminSeeder>();
    await seeder.SeedAsync();
}

app.Run();


static class FriendlyDuplicateErrorMiddlewareExtensions
{
    public static IApplicationBuilder UseFriendlyDuplicateErrors(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            try
            {
                await next();
            }
            catch (DbUpdateException ex) when (LooksLikeDuplicateError(ex))
            {
                var message = BuildDuplicateMessage(ex);
                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsJsonAsync(new
                {
                    message,
                    detail = message,
                    title = "Duplicate data",
                    status = StatusCodes.Status400BadRequest
                });
            }
        });
    }

    private static bool LooksLikeDuplicateError(Exception ex)
    {
        var text = Flatten(ex).ToLowerInvariant();
        return
            text.Contains("unique constraint") ||
            text.Contains("duplicate") ||
            text.Contains("sqlite error 19") ||
            text.Contains("cannot insert duplicate key") ||
            text.Contains("violates unique constraint");
    }

    private static string BuildDuplicateMessage(Exception ex)
    {
        var text = Flatten(ex).ToLowerInvariant();
        var parts = new List<string>();
        if (ContainsAny(text, "username", "user_name", "users.username", "ix_users_username", "user name"))
            parts.Add("اسم المستخدم مستخدم بالفعل");
        if (ContainsAny(text, "studentcode", "student_code", "students.studentcode", "ix_students_studentcode", "student code"))
            parts.Add("كود الطالب مستخدم بالفعل");
        if (ContainsAny(text, "nationalid", "national_id", "students.nationalid", "ix_students_nationalid", "national id"))
            parts.Add("الرقم القومي / الهوية مستخدم بالفعل");
        if (ContainsAny(text, "mobile", "phone", "phonenumber", "phone_number"))
            parts.Add("رقم الجوال مستخدم بالفعل");
        if (parts.Count == 0)
            parts.Add("توجد بيانات مكررة. راجع اسم المستخدم أو كود الطالب أو الرقم القومي / الهوية.");
        return string.Join("، ", parts);
    }

    private static bool ContainsAny(string text, params string[] values)
        => values.Any(v => text.Contains(v.ToLowerInvariant()));

    private static string Flatten(Exception ex)
    {
        var messages = new List<string>();
        var current = ex;
        while (current is not null)
        {
            messages.Add(current.Message);
            current = current.InnerException;
        }
        return string.Join(" | ", messages);
    }
}


static class FriendlyStudentErrorMiddlewareExtensions
{
    public static IApplicationBuilder UseFriendlyStudentErrors(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            try
            {
                await next();
            }
            catch (InvalidOperationException ex) when (IsKnownStudentDuplicate(ex))
            {
                var message = ToArabicStudentDuplicateMessage(ex.Message);

                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/json; charset=utf-8";

                await context.Response.WriteAsJsonAsync(new
                {
                    message,
                    detail = message,
                    title = "Student duplicate data",
                    status = StatusCodes.Status400BadRequest
                });
            }
        });
    }

    private static bool IsKnownStudentDuplicate(InvalidOperationException ex)
    {
        var message = ex.Message.ToLowerInvariant();

        return
            message.Contains("student code already exists") ||
            message.Contains("username already exists") ||
            message.Contains("user name already exists") ||
            message.Contains("national id already exists") ||
            message.Contains("nationalid already exists") ||
            message.Contains("mobile already exists") ||
            message.Contains("phone already exists") ||
            message.Contains("already exists");
    }

    private static string ToArabicStudentDuplicateMessage(string raw)
    {
        var message = raw.ToLowerInvariant();

        if (message.Contains("student code"))
            return "كود الطالب مستخدم بالفعل. الرجاء إدخال كود مختلف."; 

        if (message.Contains("username") || message.Contains("user name"))
            return "اسم المستخدم مستخدم بالفعل. الرجاء إدخال اسم مستخدم مختلف."; 

        if (message.Contains("national id") || message.Contains("nationalid"))
            return "الرقم القومي / الهوية مستخدم بالفعل. الرجاء إدخال رقم مختلف."; 

        if (message.Contains("mobile") || message.Contains("phone"))
            return "رقم الجوال مستخدم بالفعل. الرجاء إدخال رقم مختلف."; 

        return "توجد بيانات مكررة لهذا الطالب. راجع كود الطالب أو اسم المستخدم أو الرقم القومي / الهوية."; 
    }
}

