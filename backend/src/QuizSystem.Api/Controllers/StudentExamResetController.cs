using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizSystem.Infrastructure.Persistence;

namespace QuizSystem.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin/exam-attempts")]
public sealed class StudentExamResetController : ControllerBase
{
    private readonly AppDbContext _db;

    public StudentExamResetController(AppDbContext db)
    {
        _db = db;
    }

    [HttpDelete("{attemptId:guid}/reset-with-snapshots")]
    public async Task<IActionResult> ResetAttemptWithSnapshots(Guid attemptId, CancellationToken cancellationToken)
    {
        var attempt = await _db.Attempts
            .FirstOrDefaultAsync(x => x.Id == attemptId, cancellationToken);

        if (attempt == null)
            return NotFound("Attempt not found");

        var answers = await _db.AttemptAnswers
            .Where(x => x.ExamAttemptId == attemptId)
            .ToListAsync(cancellationToken);

        if (answers.Count > 0)
            _db.AttemptAnswers.RemoveRange(answers);

        var snapshots = await _db.ExamAttemptQuestionSnapshots
            .Where(x => x.ExamAttemptId == attemptId)
            .ToListAsync(cancellationToken);

        if (snapshots.Count > 0)
            _db.ExamAttemptQuestionSnapshots.RemoveRange(snapshots);

        _db.Attempts.Remove(attempt);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Attempt, answers and snapshots reset successfully", attemptId });
    }
}
