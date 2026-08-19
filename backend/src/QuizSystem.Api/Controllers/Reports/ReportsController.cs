using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizSystem.Application.Contracts.Attempts;
using QuizSystem.Application.Contracts.Auth;
using QuizSystem.Application.Contracts.Exams;
using QuizSystem.Application.Contracts.Imports;
using QuizSystem.Application.Contracts.Portals;
using QuizSystem.Application.Contracts.Reports;
using QuizSystem.Application.DTOs;
using QuizSystem.Infrastructure.Persistence;
using QuizSystem.Infrastructure.Services;

namespace QuizSystem.Api.Controllers.Reports;
[ApiController]
[Route("api/reports")]
[Authorize(Policy = "AdminOrSupervisor")]
public class ReportsController : ControllerBase
{
    private readonly IReportPdfService _reportPdfService;

    public ReportsController(IReportPdfService reportPdfService)
    {
        _reportPdfService = reportPdfService;
    }

    // REMOVED OLD PDF ENDPOINT
    
}
