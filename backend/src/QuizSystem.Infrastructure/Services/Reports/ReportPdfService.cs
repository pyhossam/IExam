using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuizSystem.Application.Contracts.Attempts;
using QuizSystem.Application.Contracts.Auth;
using QuizSystem.Application.Contracts.Exams;
using QuizSystem.Application.Contracts.Imports;
using QuizSystem.Application.Contracts.Portals;
using QuizSystem.Application.Contracts.Reports;
using QuizSystem.Domain.Entities;
using QuizSystem.Infrastructure.Persistence;

namespace QuizSystem.Infrastructure.Services.Reports;
public class ReportPdfService : IReportPdfService
{
    private readonly AppDbContext _db;

    public ReportPdfService(AppDbContext db)
    {
        _db = db;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> BuildStudentReportPdfAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var student = await _db.Students
            .Include(x => x.ExamAttempts)
                .ThenInclude(x => x.Exam)
            .FirstOrDefaultAsync(x => x.Id == studentId, cancellationToken)
            ?? throw new InvalidOperationException("Student not found");

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(25);
                page.Size(PageSizes.A4);

                page.Header().Column(col =>
                {
                    col.Item().Text("Student Report").Bold().FontSize(20);
                    col.Item().Text($"Name: {student.FullName}");
                    col.Item().Text($"Code: {student.StudentCode}");
                    col.Item().Text($"Grade: {student.Grade}");
                });

                page.Content().Column(col =>
                {
                    foreach (var attempt in student.ExamAttempts.OrderByDescending(x => x.SubmittedAtUtc))
                    {
                        col.Item().PaddingTop(10).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(inner =>
                        {
                            inner.Item().Text($"Exam: {attempt.Exam.Title}").Bold();
                            inner.Item().Text($"Code: {attempt.Exam.ExamCode}");
                            inner.Item().Text($"Score: {attempt.Score}/{attempt.TotalQuestions}");
                            inner.Item().Text($"Percentage: {attempt.Percentage}%");
                            inner.Item().Text($"Submitted: {attempt.SubmittedAtUtc}");
                        });
                    }
                });

                page.Footer().AlignCenter().Text("QuizSystem - Student Report");
            });
        }).GeneratePdf();
    }

    public async Task<byte[]> BuildExamReportPdfAsync(Guid examId, CancellationToken cancellationToken = default)
    {
        var exam = await _db.Exams
            .Include(x => x.Attempts)
                .ThenInclude(x => x.StudentProfile)
            .FirstOrDefaultAsync(x => x.Id == examId, cancellationToken)
            ?? throw new InvalidOperationException("Exam not found");

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(25);
                page.Size(PageSizes.A4);

                page.Header().Column(col =>
                {
                    col.Item().Text("Exam Report").Bold().FontSize(20);
                    col.Item().Text($"Title: {exam.Title}");
                    col.Item().Text($"Code: {exam.ExamCode}");
                    col.Item().Text($"Start: {exam.StartAtUtc}");
                    col.Item().Text($"End: {exam.EndAtUtc}");
                });

                page.Content().Column(col =>
                {
                    foreach (var attempt in exam.Attempts.OrderByDescending(x => x.Percentage))
                    {
                        col.Item().PaddingTop(10).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(inner =>
                        {
                            inner.Item().Text($"Student: {attempt.StudentProfile.FullName}").Bold();
                            inner.Item().Text($"Student Code: {attempt.StudentProfile.StudentCode}");
                            inner.Item().Text($"Score: {attempt.Score}/{attempt.TotalQuestions}");
                            inner.Item().Text($"Percentage: {attempt.Percentage}%");
                            inner.Item().Text($"Submitted: {attempt.SubmittedAtUtc}");
                        });
                    }
                });

                page.Footer().AlignCenter().Text("QuizSystem - Exam Report");
            });
        }).GeneratePdf();
    }
}
