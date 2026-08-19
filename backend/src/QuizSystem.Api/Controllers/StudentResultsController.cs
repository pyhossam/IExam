using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using QuizSystem.Infrastructure.Persistence;

namespace QuizSystem.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/student/exams/results")]
public sealed class StudentResultsController : ControllerBase
{
    private readonly AppDbContext _db;

    public StudentResultsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyResults(CancellationToken cancellationToken)
    {
        var userIdText =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("userId")
            ?? User.FindFirstValue("id");

        if (!Guid.TryParse(userIdText, out var userId))
            return Unauthorized();

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user == null || user.StudentProfileId == null)
            return Unauthorized();

        var studentId = user.StudentProfileId.Value;

        var results = await _db.Attempts
            .AsNoTracking()
            .Where(a =>
                a.StudentProfileId == studentId &&
                a.SubmittedAtUtc != null)
            .OrderByDescending(a => a.SubmittedAtUtc)
            .Select(a => new
            {
                examId = a.ExamId,
                attemptId = a.Id,
                title = a.Exam.Title,
                examCode = a.Exam.ExamCode,
                score = a.Score,
                totalQuestions = a.TotalQuestions,
                percentage = a.Percentage,
                submittedAtUtc = a.SubmittedAtUtc,
                isSubmitted = true,
                canStart = false,
                availabilityStatus = "تم التسليم"
            })
            .ToListAsync(cancellationToken);

        return Ok(results);
    }
}
