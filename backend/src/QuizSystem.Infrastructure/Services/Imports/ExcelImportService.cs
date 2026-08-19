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
using QuizSystem.Domain.Enums;
using QuizSystem.Infrastructure.Persistence;

namespace QuizSystem.Infrastructure.Services.Imports;
public class ExcelImportService : IExcelImportService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public ExcelImportService(AppDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<UploadStudentsResultDto> UploadStudentsAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        var result = new UploadStudentsResultDto();
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (extension == ".xlsx")
        {
            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheet(1);
            var rows = sheet.RangeUsed()?.RowsUsed().ToList() ?? new List<IXLRangeRow>();
            if (rows.Count <= 1) return result;

            var headers = rows[0].Cells().Select(c => c.GetString().Trim().ToLowerInvariant()).ToList();
            var map = BuildHeaderMap(headers, "full_name", "student_code", "grade");

            foreach (var row in rows.Skip(1))
            {
                try
                {
                    var fullName = ReadCell(row, map, "full_name");
                    var studentCode = ReadCell(row, map, "student_code");
                    var grade = ReadCell(row, map, "grade");
                    var username = ReadOptionalCell(row, map, "username");
                    var password = ReadOptionalCell(row, map, "password");

                    if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(studentCode))
                        throw new InvalidOperationException("full_name and student_code are required");

                    if (await _db.Students.AnyAsync(x => x.StudentCode == studentCode, cancellationToken))
                    {
                        result.Skipped++;
                        continue;
                    }

                    var student = new StudentProfile
                    {
                        FullName = fullName,
                        StudentCode = studentCode,
                        Grade = grade,
                        IsActive = true
                    };

                    _db.Students.Add(student);
                    await _db.SaveChangesAsync(cancellationToken);

                    if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                    {
                        if (!await _db.Users.AnyAsync(x => x.UserName == username, cancellationToken))
                        {
                            _db.Users.Add(new AppUser
                            {
                                UserName = username,
                                PasswordHash = _passwordHasher.Hash(password),
                                Role = UserRole.Student,
                                StudentProfileId = student.Id,
                                IsActive = true
                            });
                            await _db.SaveChangesAsync(cancellationToken);
                        }
                    }

                    result.Inserted++;
                }
                catch (Exception ex)
                {
                    result.Skipped++;
                    result.Errors.Add(ex.Message);
                }
            }
        }
        else if (extension == ".csv")
        {
            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream);
            var lines = new List<string>();
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (line is not null) lines.Add(line);
            }

            if (lines.Count <= 1) return result;

            var headers = lines[0].Split(',').Select(x => x.Trim().ToLowerInvariant()).ToList();
            var map = BuildHeaderMap(headers, "full_name", "student_code", "grade");

            foreach (var line in lines.Skip(1))
            {
                try
                {
                    var cols = line.Split(',');
                    var fullName = GetValue(cols, map, "full_name");
                    var studentCode = GetValue(cols, map, "student_code");
                    var grade = GetValue(cols, map, "grade");
                    var username = GetOptionalValue(cols, map, "username");
                    var password = GetOptionalValue(cols, map, "password");

                    if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(studentCode))
                        throw new InvalidOperationException("full_name and student_code are required");

                    if (await _db.Students.AnyAsync(x => x.StudentCode == studentCode, cancellationToken))
                    {
                        result.Skipped++;
                        continue;
                    }

                    var student = new StudentProfile
                    {
                        FullName = fullName,
                        StudentCode = studentCode,
                        Grade = grade,
                        IsActive = true
                    };

                    _db.Students.Add(student);
                    await _db.SaveChangesAsync(cancellationToken);

                    if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                    {
                        if (!await _db.Users.AnyAsync(x => x.UserName == username, cancellationToken))
                        {
                            _db.Users.Add(new AppUser
                            {
                                UserName = username,
                                PasswordHash = _passwordHasher.Hash(password),
                                Role = UserRole.Student,
                                StudentProfileId = student.Id,
                                IsActive = true
                            });
                            await _db.SaveChangesAsync(cancellationToken);
                        }
                    }

                    result.Inserted++;
                }
                catch (Exception ex)
                {
                    result.Skipped++;
                    result.Errors.Add(ex.Message);
                }
            }
        }
        else
        {
            throw new InvalidOperationException("Only .xlsx and .csv are supported");
        }

        return result;
    }

    public async Task<UploadRegistrationsResultDto> UploadRegistrationsAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        var result = new UploadRegistrationsResultDto();
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (extension == ".xlsx")
        {
            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheet(1);
            var rows = sheet.RangeUsed()?.RowsUsed().ToList() ?? new List<IXLRangeRow>();
            if (rows.Count <= 1) return result;

            var headers = rows[0].Cells().Select(c => c.GetString().Trim().ToLowerInvariant()).ToList();
            var map = BuildHeaderMap(headers, "student_code", "exam_code");

            foreach (var row in rows.Skip(1))
            {
                try
                {
                    var studentCode = ReadCell(row, map, "student_code");
                    var examCode = ReadCell(row, map, "exam_code");

                    var student = await _db.Students.FirstOrDefaultAsync(x => x.StudentCode == studentCode, cancellationToken)
                        ?? throw new InvalidOperationException($"Student not found: {studentCode}");

                    var exam = await _db.Exams.FirstOrDefaultAsync(x => x.ExamCode == examCode, cancellationToken)
                        ?? throw new InvalidOperationException($"Exam not found: {examCode}");

                    var exists = await _db.Registrations.AnyAsync(
                        x => x.StudentProfileId == student.Id && x.ExamId == exam.Id,
                        cancellationToken
                    );

                    if (exists)
                    {
                        result.Skipped++;
                        continue;
                    }

                    _db.Registrations.Add(new ExamRegistration
                    {
                        StudentProfileId = student.Id,
                        ExamId = exam.Id,
                        AssignedByUserId = Guid.Empty,
                        IsActive = true
                    });

                    await _db.SaveChangesAsync(cancellationToken);
                    result.Inserted++;
                }
                catch (Exception ex)
                {
                    result.Skipped++;
                    result.Errors.Add(ex.Message);
                }
            }
        }
        else if (extension == ".csv")
        {
            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream);
            var lines = new List<string>();
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (line is not null) lines.Add(line);
            }

            if (lines.Count <= 1) return result;

            var headers = lines[0].Split(',').Select(x => x.Trim().ToLowerInvariant()).ToList();
            var map = BuildHeaderMap(headers, "student_code", "exam_code");

            foreach (var line in lines.Skip(1))
            {
                try
                {
                    var cols = line.Split(',');
                    var studentCode = GetValue(cols, map, "student_code");
                    var examCode = GetValue(cols, map, "exam_code");

                    var student = await _db.Students.FirstOrDefaultAsync(x => x.StudentCode == studentCode, cancellationToken)
                        ?? throw new InvalidOperationException($"Student not found: {studentCode}");

                    var exam = await _db.Exams.FirstOrDefaultAsync(x => x.ExamCode == examCode, cancellationToken)
                        ?? throw new InvalidOperationException($"Exam not found: {examCode}");

                    var exists = await _db.Registrations.AnyAsync(
                        x => x.StudentProfileId == student.Id && x.ExamId == exam.Id,
                        cancellationToken
                    );

                    if (exists)
                    {
                        result.Skipped++;
                        continue;
                    }

                    _db.Registrations.Add(new ExamRegistration
                    {
                        StudentProfileId = student.Id,
                        ExamId = exam.Id,
                        AssignedByUserId = Guid.Empty,
                        IsActive = true
                    });

                    await _db.SaveChangesAsync(cancellationToken);
                    result.Inserted++;
                }
                catch (Exception ex)
                {
                    result.Skipped++;
                    result.Errors.Add(ex.Message);
                }
            }
        }
        else
        {
            throw new InvalidOperationException("Only .xlsx and .csv are supported");
        }

        return result;
    }

    public byte[] BuildStudentsTemplate()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Students");

        sheet.Cell(1, 1).Value = "full_name";
        sheet.Cell(1, 2).Value = "student_code";
        sheet.Cell(1, 3).Value = "grade";
        sheet.Cell(1, 4).Value = "username";
        sheet.Cell(1, 5).Value = "password";

        sheet.Cell(2, 1).Value = "Ahmed Ali";
        sheet.Cell(2, 2).Value = "STD1001";
        sheet.Cell(2, 3).Value = "Grade 6";
        sheet.Cell(2, 4).Value = "student.ahmed";
        sheet.Cell(2, 5).Value = "Student@123";

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] BuildRegistrationsTemplate()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Registrations");

        sheet.Cell(1, 1).Value = "student_code";
        sheet.Cell(1, 2).Value = "exam_code";

        sheet.Cell(2, 1).Value = "STD1001";
        sheet.Cell(2, 2).Value = "EXAM001";

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static Dictionary<string, int> BuildHeaderMap(List<string> headers, params string[] required)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Count; i++)
            map[headers[i]] = i;

        foreach (var item in required)
        {
            if (!map.ContainsKey(item))
                throw new InvalidOperationException($"Missing required column: {item}");
        }

        return map;
    }

    private static string ReadCell(IXLRangeRow row, Dictionary<string, int> map, string key)
    {
        var cellIndex = map[key] + 1;
        return row.Cell(cellIndex).GetString().Trim();
    }

    private static string? ReadOptionalCell(IXLRangeRow row, Dictionary<string, int> map, string key)
    {
        if (!map.ContainsKey(key)) return null;
        var cellIndex = map[key] + 1;
        return row.Cell(cellIndex).GetString().Trim();
    }

    private static string GetValue(string[] cols, Dictionary<string, int> map, string key)
    {
        if (!map.ContainsKey(key))
            throw new InvalidOperationException($"Missing required column: {key}");

        var index = map[key];
        if (index >= cols.Length)
            return string.Empty;

        return cols[index].Trim();
    }

    private static string? GetOptionalValue(string[] cols, Dictionary<string, int> map, string key)
    {
        if (!map.ContainsKey(key))
            return null;

        var index = map[key];
        if (index >= cols.Length)
            return null;

        return cols[index].Trim();
    }
}
