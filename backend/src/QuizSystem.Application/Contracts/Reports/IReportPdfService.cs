using QuizSystem.Application.DTOs;

namespace QuizSystem.Application.Contracts.Reports;
public interface IReportPdfService
{
    Task<byte[]> BuildStudentReportPdfAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<byte[]> BuildExamReportPdfAsync(Guid examId, CancellationToken cancellationToken = default);
}
