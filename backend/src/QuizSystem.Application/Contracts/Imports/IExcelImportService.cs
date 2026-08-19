using Microsoft.AspNetCore.Http;
using QuizSystem.Application.DTOs;

namespace QuizSystem.Application.Contracts.Imports;
public interface IExcelImportService
{
    Task<UploadStudentsResultDto> UploadStudentsAsync(IFormFile file, CancellationToken cancellationToken = default);
    Task<UploadRegistrationsResultDto> UploadRegistrationsAsync(IFormFile file, CancellationToken cancellationToken = default);

    byte[] BuildStudentsTemplate();
    byte[] BuildRegistrationsTemplate();
}
