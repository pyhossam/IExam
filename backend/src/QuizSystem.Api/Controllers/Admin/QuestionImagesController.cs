using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace QuizSystem.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/exams/questions/images")]
[Authorize(Policy = "AdminOnly")]
public class QuestionImagesController : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    [HttpPost]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "No file uploaded",
                Detail = "لم يتم رفع أي ملف",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var ext = Path.GetExtension(file.FileName);

        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid image type",
                Detail = "نوع الصورة غير مدعوم. الأنواع المسموحة: jpg, jpeg, png, webp",
                Status = StatusCodes.Status400BadRequest
            });
        }

        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid content type",
                Detail = "الملف المرفوع ليس صورة",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var folder = Path.Combine(webRoot, "uploads", "exam-images");
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var fullPath = Path.Combine(folder, fileName);

        await using var stream = System.IO.File.Create(fullPath);
        await file.CopyToAsync(stream, cancellationToken);

        var url = $"/uploads/exam-images/{fileName}";

        return Ok(new
        {
            url,
            fileName
        });
    }
}
