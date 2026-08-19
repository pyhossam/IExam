using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using QuizSystem.Application.Contracts.Attempts;
using QuizSystem.Application.Contracts.Auth;
using QuizSystem.Application.Contracts.Exams;
using QuizSystem.Application.Contracts.Imports;
using QuizSystem.Application.Contracts.Portals;
using QuizSystem.Application.Contracts.Reports;
using QuizSystem.Application.DTOs;
using QuizSystem.Domain.Entities;
using QuizSystem.Infrastructure.Persistence;
using QuestPDF.Infrastructure;
using System.IO.Compression;
namespace QuizSystem.Infrastructure.Services.Exams;

public class ExamPdfService: IExamPdfService
{
    private readonly AppDbContext _db;
    private const string ArabicFont = "Tajawal";

    public ExamPdfService(AppDbContext db)
    {
        _db = db;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    

    //private static string ToArabicNumber(int value)
    //{
    //    return ToArabicDigits(value);
    //}

    private static string PdfText(string? value)
    {
        return ToArabicDigits(value ?? string.Empty);
    }



    public async Task<byte[]> ExportQuestionsPdfAsync(Guid examId, bool withAnswers, CancellationToken cancellationToken = default)
    {
        var exam = await LoadExamAsync(examId, cancellationToken);
        var questions = exam.Questions.OrderBy(x => x.CreatedAtUtc).ToList();

        return BuildSingleQuestionsPdf(exam, questions, withAnswers);
    }

    public async Task<byte[]> ExportRandomFormsPdfAsync(Guid examId, int formsCount, CancellationToken cancellationToken = default)
    {
        var exam = await LoadExamAsync(examId, cancellationToken);
        var forms = BuildRandomForms(exam, formsCount);

        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            foreach (var form in forms)
            {
                var pdfBytes = BuildSingleFormPdf(exam, form);

                var entry = archive.CreateEntry($"Form_{form.Label}.pdf", CompressionLevel.Fastest);
                using var entryStream = entry.Open();
                using var pdfStream = new MemoryStream(pdfBytes);
                pdfStream.CopyTo(entryStream);
            }
        }

        return stream.ToArray();
    }

    public async Task<byte[]> ExportRandomFormsAnswerKeysPdfAsync(Guid examId, int formsCount, CancellationToken cancellationToken = default)
    {
        var exam = await LoadExamAsync(examId, cancellationToken);
        var forms = BuildRandomForms(exam, formsCount);

        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            foreach (var form in forms)
            {
                var pdfBytes = BuildSingleAnswerKeyPdf(exam, form);

                var entry = archive.CreateEntry($"AnswerKey_{form.Label}.pdf", CompressionLevel.Fastest);
                using var entryStream = entry.Open();
                using var pdfStream = new MemoryStream(pdfBytes);
                pdfStream.CopyTo(entryStream);
            }
        }

        return stream.ToArray();
    }

    private byte[] BuildSingleQuestionsPdf(Exam exam, List<ExamQuestion> questions, bool withAnswers)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(18);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily(ArabicFont).FontSize(12).FontColor(Colors.Black));

                page.Header().Element(header => BuildOfficialHeader(header, exam, null));

                page.Content().Element(content =>
                {
                    content.Column(col =>
                    {
                        col.Item()
                            .PaddingTop(4)
                            .PaddingBottom(6)
                            .AlignRight()
                            .Text(text =>
                            {
                                text.Span("تعليمات الاختبار: أجب عن جميع الأسئلة واختر الإجابة الصحيحة.")
                                    .FontFamily(ArabicFont)
                                    .FontSize(11)
                                    .SemiBold();
                            });

                        int index = 1;
                        foreach (var q in questions)
                        {
                            var current = index;

                            col.Item()
                                .PaddingBottom(6)
                                .ShowEntire()
                                .Element(item =>
                                {
                                    BuildQuestionCard(
                                        item,
                                        current,
                                        q.QuestionText,
                                        q.ChoiceA,
                                        q.ChoiceB,
                                        q.ChoiceC,
                                        q.ChoiceD,
                                        withAnswers ? q.CorrectAnswer : null,
                                        withAnswers ? q.Explanation : null
                                    );
                                });

                            index++;
                        }
                    });
                });

                page.Footer().Element(footer => BuildStandardFooter(footer, null, "ورقة اختبار رسمية"));
            });
        }).GeneratePdf();
    }

    private byte[] BuildSingleFormPdf(Exam exam, FormModel form)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(18);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily(ArabicFont).FontSize(12).FontColor(Colors.Black));

                page.Header().Element(header => BuildOfficialHeader(header, exam, form.Label));

                page.Content().Element(content =>
                {
                    content.Column(col =>
                    {
                        col.Item()
                            .PaddingTop(4)
                            .PaddingBottom(6)
                            .AlignRight()
                            .Text(text =>
                            {
                                text.Span("تعليمات الاختبار: أجب عن جميع الأسئلة واختر الإجابة الصحيحة.")
                                    .FontFamily(ArabicFont)
                                    .FontSize(11)
                                    .SemiBold();
                            });

                        int index = 1;
                        foreach (var q in form.Questions)
                        {
                            var current = index;

                            col.Item()
                                .PaddingBottom(6)
                                .ShowEntire()
                                .Element(item =>
                                {
                                    BuildQuestionCard(
                                        item,
                                        current,
                                        q.QuestionText,
                                        q.ChoiceA,
                                        q.ChoiceB,
                                        q.ChoiceC,
                                        q.ChoiceD,
                                        null,
                                        null
                                    );
                                });

                            index++;
                        }
                    });
                });

                page.Footer().Element(footer => BuildStandardFooter(footer, form.Label, "ورقة اختبار رسمية"));
            });
        }).GeneratePdf();
    }

    private byte[] BuildSingleAnswerKeyPdf(Exam exam, FormModel form)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(18);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily(ArabicFont).FontSize(12).FontColor(Colors.Black));

                page.Header().Element(header =>
                {
                    header
                        .Border(1)
                        .BorderColor(Colors.Grey.Lighten2)
                        .PaddingVertical(8)
                        .PaddingHorizontal(12)
                        .Column(col =>
                        {
                            col.Spacing(3);

                            col.Item().AlignRight().Text(text =>
                            {
                                text.Span("نظام الاختبارات الذكي").FontFamily(ArabicFont).Bold().FontSize(16);
                            });

                            col.Item().AlignRight().Text(text =>
                            {
                                text.Span(PdfText(exam.Title)).FontFamily(ArabicFont).Bold().FontSize(14);
                            });

                            col.Item().Element(item => BuildLabelValueRow(item, "كود الاختبار", exam.ExamCode, false));
                            col.Item().Element(item => BuildLabelValueRow(item, "نموذج", form.Label, true));

                            col.Item().AlignRight().Text(text =>
                            {
                                text.Span("مفتاح الإجابة").FontFamily(ArabicFont).SemiBold().FontSize(11);
                            });
                        });
                });

                page.Content().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(2);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellHeaderStyle).AlignCenter().Text(text =>
                        {
                            text.Span("رقم السؤال").FontFamily(ArabicFont).Bold();
                        });

                        header.Cell().Element(CellHeaderStyle).AlignCenter().Text(text =>
                        {
                            text.Span("الإجابة الصحيحة").FontFamily(ArabicFont).Bold();
                        });
                    });

                    int index = 1;
                    foreach (var q in form.Questions)
                    {
                        table.Cell().Element(CellBodyStyle).AlignCenter().Text(text =>
                        {
                            text.Span(ToArabicNumber(index)).FontFamily(ArabicFont);
                        });

                        table.Cell().Element(CellBodyStyle).AlignCenter().Text(text =>
                        {
                            text.Span(q.CorrectAnswer).FontFamily(ArabicFont).SemiBold();
                        });

                        index++;
                    }
                });

                page.Footer().Element(footer => BuildStandardFooter(footer, form.Label, "مفتاح إجابة رسمي"));
            });
        }).GeneratePdf();
    }

    private void BuildOfficialHeader(IContainer container, Exam exam, string? formLabel)
    {
        container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(8)
            .PaddingHorizontal(12)
            .Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(left =>
                    {
                        left.Spacing(4);

                        left.Item().Element(item => BuildHeaderField(item, "الاسم"));
                        left.Item().Element(item => BuildHeaderField(item, "الصف"));
                        left.Item().Element(item => BuildHeaderField(item, "التاريخ"));
                    });

                    row.ConstantItem(20);

                    row.RelativeItem().Column(right =>
                    {
                        right.Spacing(3);

                        right.Item().AlignRight().Text(text =>
                        {
                            text.Span("نظام الاختبارات الذكي").FontFamily(ArabicFont).Bold().FontSize(16);
                        });

                        right.Item().AlignRight().Text(text =>
                        {
                            text.Span(PdfText(exam.Title)).FontFamily(ArabicFont).Bold().FontSize(14);
                        });

                        right.Item().Element(item => BuildLabelValueRow(item, "كود الاختبار", exam.ExamCode, false));

                        if (!string.IsNullOrWhiteSpace(formLabel))
                            right.Item().Element(item => BuildLabelValueRow(item, "نموذج", formLabel, true));
                    });
                });
            });
    }

    private void BuildHeaderField(IContainer container, string label)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(1);
                columns.ConstantColumn(10);
                columns.ConstantColumn(55);
            });

            table.Cell().AlignRight().Text(text =>
            {
                text.Span("..............................")
                    .FontFamily(ArabicFont)
                    .FontSize(11);
            });

            table.Cell().AlignCenter().Text(text =>
            {
                text.Span(":")
                    .FontFamily(ArabicFont)
                    .FontSize(11);
            });

            table.Cell().AlignRight().Text(text =>
            {
                text.Span(label)
                    .FontFamily(ArabicFont)
                    .SemiBold()
                    .FontSize(11);
            });
        });
    }

    private void BuildLabelValueRow(IContainer container, string label, string value, bool boldValue)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(1);
                columns.ConstantColumn(10);
                columns.ConstantColumn(78);
            });

            table.Cell().AlignRight().Text(text =>
            {
                var span = text.Span(PdfText(value)).FontFamily(ArabicFont).FontSize(11);
                if (boldValue)
                    span.Bold();
            });

            table.Cell().AlignCenter().Text(text =>
            {
                text.Span(":")
                    .FontFamily(ArabicFont)
                    .FontSize(11);
            });

            table.Cell().AlignRight().Text(text =>
            {
                var span = text.Span(label).FontFamily(ArabicFont).FontSize(11);
                if (boldValue)
                    span.Bold();
            });
        });
    }



    private static readonly Regex FractionRegex = new(
        @"(?<![\p{L}\p{N}])([0-9٠-٩]+)\s*/\s*([0-9٠-٩]+)(?![\p{L}\p{N}])",
        RegexOptions.Compiled);

    private sealed class PdfTextPart
    {
        public string Text { get; set; } = string.Empty;
        public bool IsFraction { get; set; }
        public string Numerator { get; set; } = string.Empty;
        public string Denominator { get; set; } = string.Empty;
    }

    private static string ToArabicDigits(object? value)
    {
        if (value is null)
            return string.Empty;

        return value.ToString()!
            .Replace("0", "٠")
            .Replace("1", "١")
            .Replace("2", "٢")
            .Replace("3", "٣")
            .Replace("4", "٤")
            .Replace("5", "٥")
            .Replace("6", "٦")
            .Replace("7", "٧")
            .Replace("8", "٨")
            .Replace("9", "٩");
    }

    private static string ToArabicNumber(int value)
    {
        return ToArabicDigits(value);
    }

    

    private static List<PdfTextPart> ParsePdfTextParts(string? value)
    {
        var source = value ?? string.Empty;
        var parts = new List<PdfTextPart>();
        var lastIndex = 0;

        foreach (Match match in FractionRegex.Matches(source))
        {
            if (match.Index > lastIndex)
            {
                parts.Add(new PdfTextPart
                {
                    Text = source.Substring(lastIndex, match.Index - lastIndex)
                });
            }

            parts.Add(new PdfTextPart
            {
                IsFraction = true,
                Numerator = match.Groups[1].Value,
                Denominator = match.Groups[2].Value
            });

            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < source.Length)
        {
            parts.Add(new PdfTextPart
            {
                Text = source.Substring(lastIndex)
            });
        }

        return parts;
    }

    private void BuildFraction(
        IContainer container,
        string numerator,
        string denominator,
        float fontSize,
        bool bold,
        string? fontColor)
    {
        container
            .MinWidth(18)
            .PaddingHorizontal(2)
            .Column(col =>
            {
                col.Spacing(0);

                col.Item().AlignCenter().Text(text =>
                {
                    var span = text.Span(ToArabicDigits(numerator))
                        .FontFamily(ArabicFont)
                        .FontSize(fontSize - 1);

                    if (bold) span.Bold();
                    span.FontColor(string.IsNullOrWhiteSpace(fontColor) ? Colors.Black : fontColor);
                });

                col.Item()
                    .PaddingVertical(1)
                    .LineHorizontal(1)
                    .LineColor(string.IsNullOrWhiteSpace(fontColor) ? Colors.Black : fontColor);

                col.Item().AlignCenter().Text(text =>
                {
                    var span = text.Span(ToArabicDigits(denominator))
                        .FontFamily(ArabicFont)
                        .FontSize(fontSize - 1);

                    if (bold) span.Bold();
                    span.FontColor(string.IsNullOrWhiteSpace(fontColor) ? Colors.Black : fontColor);
                });
            });
    }

    private void BuildRichArabicText(
        IContainer container,
        string? value,
        float fontSize,
        bool bold = false,
        string? fontColor = null)
    {
        var parts = ParsePdfTextParts(value);

        if (!parts.Any(x => x.IsFraction))
        {
            container.AlignRight().Text(text =>
            {
                var span = text.Span(PdfText(value))
                    .FontFamily(ArabicFont)
                    .FontSize(fontSize);

                if (bold) span.Bold();
                span.FontColor(string.IsNullOrWhiteSpace(fontColor) ? Colors.Black : fontColor);
            });

            return;
        }

        container.AlignRight().Row(row =>
        {
            row.Spacing(3);

            foreach (var part in parts.AsEnumerable().Reverse())
            {
                if (part.IsFraction)
                {
                    row.AutoItem()
                        .AlignMiddle()
                        .Element(item => BuildFraction(
                            item,
                            part.Numerator,
                            part.Denominator,
                            fontSize,
                            bold,
                            fontColor));
                }
                else if (!string.IsNullOrWhiteSpace(part.Text))
                {
                    row.AutoItem().AlignMiddle().Text(text =>
                    {
                        var span = text.Span(PdfText(part.Text))
                            .FontFamily(ArabicFont)
                            .FontSize(fontSize);

                        if (bold) span.Bold();
                        span.FontColor(string.IsNullOrWhiteSpace(fontColor) ? Colors.Black : fontColor);
                    });
                }
            }
        });
    }

    private void BuildQuestionTitle(IContainer container, int index, string questionText)
    {
        container.Row(row =>
        {
            row.Spacing(4);

            row.RelativeItem()
                .Element(item => BuildRichArabicText(item, questionText, 13, true));

            row.AutoItem().AlignTop().Text(text =>
            {
                text.Span("\u200E" + ToArabicNumber(index) + " -\u200E")
                    .FontFamily(ArabicFont)
                    .Bold()
                    .FontSize(13);
            });
        });
    }


    private void BuildQuestionCard(
        IContainer container,
        int index,
        string questionText,
        string choiceA,
        string choiceB,
        string choiceC,
        string choiceD,
        string? correctAnswer,
        string? explanation)
    {
        container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(10)
            .Column(col =>
            {
                col.Spacing(6);

                // السؤال: الرقم أولًا قبل الشرطة، والنص يدعم الكسور العمودية
                col.Item().Element(item => BuildQuestionTitle(item, index, questionText));

                // الخيارات: تدعم الكسور العمودية داخل نص الإجابة
                col.Item().Grid(grid =>
                {
                    grid.Columns(2);
                    grid.Spacing(4);

                    grid.Item().Element(i => BuildChoiceRow(i, "B", choiceB, correctAnswer));
                    grid.Item().Element(i => BuildChoiceRow(i, "A", choiceA, correctAnswer));
                    grid.Item().Element(i => BuildChoiceRow(i, "D", choiceD, correctAnswer));
                    grid.Item().Element(i => BuildChoiceRow(i, "C", choiceC, correctAnswer));
                });

                if (!string.IsNullOrWhiteSpace(correctAnswer))
                {
                    col.Item().PaddingTop(4).AlignRight().Text(text =>
                    {
                        text.Span("الإجابة الصحيحة: ")
                            .FontFamily(ArabicFont)
                            .Bold()
                            .FontSize(12);

                        text.Span(correctAnswer)
                            .FontFamily(ArabicFont)
                            .FontSize(12)
                            .FontColor(Colors.Green.Darken2);
                    });
                }

                if (!string.IsNullOrWhiteSpace(explanation))
                {
                    col.Item().AlignRight().Row(row =>
                    {
                        row.RelativeItem()
                            .Element(item => BuildRichArabicText(item, explanation, 12));

                        row.AutoItem().Text(text =>
                        {
                            text.Span("التفسير: ")
                                .FontFamily(ArabicFont)
                                .Bold()
                                .FontSize(12);
                        });
                    });
                }
            });
    }



    private void BuildChoiceRow(
        IContainer container,
        string letter,
        string value,
        string? correctAnswer)
    {
        var isCorrectChoice =
            !string.IsNullOrWhiteSpace(correctAnswer) &&
            string.Equals(letter, correctAnswer, StringComparison.OrdinalIgnoreCase);

        container.Row(row =>
        {
            row.Spacing(6);

            // نص الخيار: يدعم الكسور العمودية بدل علامة /
            row.RelativeItem()
                .Element(item => BuildRichArabicText(
                    item,
                    value,
                    12,
                    false,
                    isCorrectChoice ? Colors.Green.Darken2 : Colors.Black));

            // حرف الخيار
            row.ConstantItem(22).AlignCenter().Text($"{letter})")
                .FontFamily(ArabicFont)
                .Bold()
                .FontSize(12);

            // مربع الاختيار
            row.ConstantItem(18)
                .AlignMiddle()
                .Width(10)
                .Height(10)
                .Border(1)
                .BorderColor(Colors.Black)
                .Background(isCorrectChoice ? Colors.Grey.Lighten2 : Colors.Transparent);
        });
    }


    private void BuildStandardFooter(IContainer container, string? formLabel, string title = "نظام الاختبارات الذكي")
    {
        var modelValue = string.IsNullOrWhiteSpace(formLabel) ? "A" : formLabel;

        container
            .BorderTop(1)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingTop(6)
            .Row(row =>
            {
                // Left side of page: نموذج | A
                row.RelativeItem().AlignLeft().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(45);
                        columns.ConstantColumn(35);
                    });

                    table.Cell()
                        .Border(1)
                        .BorderColor(Colors.Red.Medium)
                        .Padding(4)
                        .AlignCenter()
                        .Text(PdfText(modelValue))
                        .FontFamily(ArabicFont)
                        .FontSize(10)
                        .SemiBold();

                    table.Cell()
                        .Border(1)
                        .BorderColor(Colors.Red.Medium)
                        .Padding(4)
                        .AlignCenter()
                        .Text("نموذج")

                        .FontFamily(ArabicFont)
                        .FontSize(10)
                        .SemiBold();
                });

                // Center of page: نظام الاختبارات الذكي
                row.RelativeItem().AlignCenter().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(140);
                    });

                    table.Cell()
                        .Border(1)
                        .BorderColor(Colors.Grey.Lighten2)
                        .Padding(4)
                        .AlignCenter()
                        .Text(PdfText(title))
                        .FontFamily(ArabicFont)
                        .FontSize(10)
                        .SemiBold();
                });

                // Right side of page: current page | من | total pages
                row.RelativeItem().AlignRight().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(30);
                        columns.ConstantColumn(35);
                        columns.ConstantColumn(30);
                    });

                    table.Cell()
                        .Border(1)
                        .BorderColor(Colors.Grey.Lighten2)
                        .Padding(4)
                        .AlignCenter()
                        .Text(t =>
                        {
                            t.TotalPages()

                                .FontSize(10)
                                .SemiBold();
                        });

                    table.Cell()
                        .Border(1)
                        .BorderColor(Colors.Grey.Lighten2)
                        .Padding(4)
                        .AlignCenter()
                        .Text("   مــــن     ")
                        .FontFamily(ArabicFont)
                        .FontSize(10)
                        .SemiBold();

                    table.Cell()
                        .Border(1)
                        .BorderColor(Colors.Grey.Lighten2)
                        .Padding(4)
                        .AlignCenter()
                        .Text(t =>
                        {
                            t.CurrentPageNumber()

                                .FontSize(10)
                                .SemiBold();
                        });
                });
            });
    }
    private static IContainer CellHeaderStyle(IContainer container)
    {
        return container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Background(Colors.Grey.Lighten4)
            .Padding(8);
    }

    private static IContainer CellBodyStyle(IContainer container)
    {
        return container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(8);
    }

    private async Task<Exam> LoadExamAsync(Guid examId, CancellationToken cancellationToken)
    {
        return await _db.Exams
            .Include(x => x.Questions)
            .FirstOrDefaultAsync(x => x.Id == examId, cancellationToken)
            ?? throw new InvalidOperationException("Exam not found");
    }

    private static List<FormModel> BuildRandomForms(Exam exam, int formsCount)
    {
        formsCount = Math.Max(1, Math.Min(formsCount, 26));
        var labels = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
        var random = new Random();

        var forms = new List<FormModel>();

        for (int i = 0; i < formsCount; i++)
        {
            var selectedQuestions = exam.Questions
                .OrderBy(_ => random.Next())
                .Take(Math.Min(exam.ExamQuestionCount, exam.Questions.Count))
                .Select(q => ShuffleQuestion(q, random))
                .ToList();

            forms.Add(new FormModel
            {
                Label = labels[i].ToString(),
                Questions = selectedQuestions
            });
        }

        return forms;
    }

    private static FormQuestionModel ShuffleQuestion(ExamQuestion question, Random random)
    {
        var choices = new List<(string Key, string Value)>
        {
            ("A", question.ChoiceA),
            ("B", question.ChoiceB),
            ("C", question.ChoiceC),
            ("D", question.ChoiceD)
        }
        .OrderBy(_ => random.Next())
        .ToList();

        var mapped = new Dictionary<string, string>
        {
            ["A"] = choices[0].Value,
            ["B"] = choices[1].Value,
            ["C"] = choices[2].Value,
            ["D"] = choices[3].Value
        };

        var correctAnswer = choices
            .Select((x, idx) => new { OldKey = x.Key, NewKey = "ABCD"[idx].ToString() })
            .First(x => x.OldKey == question.CorrectAnswer)
            .NewKey;

        return new FormQuestionModel
        {
            QuestionText = question.QuestionText,
            ChoiceA = mapped["A"],
            ChoiceB = mapped["B"],
            ChoiceC = mapped["C"],
            ChoiceD = mapped["D"],
            CorrectAnswer = correctAnswer
        };
    }

    private class FormModel
    {
        public string Label { get; set; } = string.Empty;
        public List<FormQuestionModel> Questions { get; set; } = new();
    }

    private class FormQuestionModel
    {
        public string QuestionText { get; set; } = string.Empty;

    public string? QuestionImageUrl { get; set; }
    public string? ChoiceAImageUrl { get; set; }
    public string? ChoiceBImageUrl { get; set; }
    public string? ChoiceCImageUrl { get; set; }
    public string? ChoiceDImageUrl { get; set; }

        public string ChoiceA { get; set; } = string.Empty;
        public string ChoiceB { get; set; } = string.Empty;
        public string ChoiceC { get; set; } = string.Empty;
        public string ChoiceD { get; set; } = string.Empty;
        public string CorrectAnswer { get; set; } = string.Empty;
    }
}