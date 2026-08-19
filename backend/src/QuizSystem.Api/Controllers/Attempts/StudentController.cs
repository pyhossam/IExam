using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizSystem.Application.Contracts.Attempts;
using QuizSystem.Application.DTOs;
using System.Security.Claims;

namespace QuizSystem.Api.Controllers.Attempts;

[ApiController]
[Route("api/student")]
[Authorize(Policy = "StudentOnly")]
public class StudentController : ControllerBase
{
    private readonly IStudentExamService _studentExamService;

    public StudentController(IStudentExamService studentExamService)
    {
        _studentExamService = studentExamService;
    }

    private Guid StudentProfileId => Guid.Parse(User.FindFirstValue("studentProfileId")!);

    [HttpGet("exams/available")]
    public async Task<IActionResult> Available(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _studentExamService.GetAvailableExamsForStudentAsync(StudentProfileId, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return Problem(
                title: "Cannot load available exams",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
        catch (Exception ex)
        {
            return Problem(
                title: "Unexpected error while loading available exams",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPost("exams/{examId:guid}/start")]
    public async Task<IActionResult> Start(Guid examId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _studentExamService.StartExamAsync(StudentProfileId, examId, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return Problem(
                title: "Cannot start exam",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
        catch (Exception ex)
        {
            return Problem(
                title: "Unexpected error while starting exam",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPost("exams/submit")]
    public async Task<IActionResult> Submit([FromBody] SubmitExamRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _studentExamService.SubmitExamAsync(StudentProfileId, request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return Problem(
                title: "Cannot submit exam",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
        catch (Exception ex)
        {
            return Problem(
                title: "Unexpected error while submitting exam",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
