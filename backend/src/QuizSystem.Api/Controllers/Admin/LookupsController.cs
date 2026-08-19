using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizSystem.Application.Contracts.Lookups;

namespace QuizSystem.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/lookups")]
[Authorize(Policy = "AdminOnly")]
public class LookupsController : ControllerBase
{
    private readonly ILookupService _lookupService;

    public LookupsController(ILookupService lookupService)
    {
        _lookupService = lookupService;
    }

    [HttpGet("students")]
    public async Task<IActionResult> GetStudents(CancellationToken cancellationToken)
        => Ok(await _lookupService.GetStudentsAsync(cancellationToken));

    [HttpGet("parents")]
    public async Task<IActionResult> GetParents(CancellationToken cancellationToken)
        => Ok(await _lookupService.GetParentLookupsAsync(cancellationToken));

    [HttpGet("parent-lookups")]
    public async Task<IActionResult> GetParentLookups(CancellationToken cancellationToken)
        => Ok(await _lookupService.GetParentLookupsAsync(cancellationToken));
}
