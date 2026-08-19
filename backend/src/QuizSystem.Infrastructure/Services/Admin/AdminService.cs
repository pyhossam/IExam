using Microsoft.EntityFrameworkCore;
using QuizSystem.Application.Contracts.Attempts;
using QuizSystem.Application.Contracts.Auth;
using QuizSystem.Application.Contracts.Exams;
using QuizSystem.Application.Contracts.Imports;
using QuizSystem.Application.Contracts.Portals;
using QuizSystem.Application.Contracts.Reports;
using QuizSystem.Application.DTOs;
using QuizSystem.Application.Interfaces;
using QuizSystem.Domain.Entities;
using QuizSystem.Domain.Enums;
using QuizSystem.Infrastructure.Persistence;

namespace QuizSystem.Infrastructure.Services.Admin;
public class AdminService : IAdminService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public AdminService(AppDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<DashboardResponse> GetDashboardAsync(Guid institutionId, CancellationToken cancellationToken = default)
    {
        return new DashboardResponse
        {
            InstitutionName = await _db.Institutions
                .Where(x => x.Id == institutionId)
                .Select(x => x.Name)
                .FirstOrDefaultAsync(cancellationToken),
            Users = await _db.Users.CountAsync(x => x.InstitutionId == institutionId, cancellationToken),
            Students = await _db.Students.CountAsync(x => x.InstitutionId == institutionId, cancellationToken),
            Parents = await _db.Parents.CountAsync(x => x.InstitutionId == institutionId, cancellationToken),
            Exams = await _db.Exams.CountAsync(x => x.InstitutionId == institutionId, cancellationToken),
            Attempts = await _db.Attempts.CountAsync(x => x.InstitutionId == institutionId, cancellationToken)
        };
    }

    public async Task<Guid> CreateUserAsync(Guid institutionId, CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (await _db.Users.AnyAsync(x => x.UserName == request.UserName, cancellationToken))
            throw new InvalidOperationException("Username already exists");

        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
            throw new InvalidOperationException("Invalid role");

        var user = new AppUser
        {
            InstitutionId = institutionId,
            UserName = request.UserName,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = role,
            IsActive = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);
        return user.Id;
    }

    public async Task<Guid> CreateStudentAsync(Guid institutionId, CreateStudentRequest request, CancellationToken cancellationToken = default)
    {
        if (await _db.Students.AnyAsync(x => x.InstitutionId == institutionId && x.StudentCode == request.StudentCode, cancellationToken))
            throw new InvalidOperationException("Student code already exists");

        var student = new StudentProfile
        {
            InstitutionId = institutionId,
            FullName = request.FullName,
            StudentCode = request.StudentCode,
            Grade = request.Grade,
                Branch = request.Branch,
                NationalId = string.IsNullOrWhiteSpace(request.NationalId) ? null : request.NationalId.Trim(),
                Mobile = request.Mobile,
                Nationality = request.Nationality,
                ImagePath = request.ImagePath,
            IsActive = true
        };

        _db.Students.Add(student);
        await _db.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.UserName) && !string.IsNullOrWhiteSpace(request.Password))
        {
            if (await _db.Users.AnyAsync(x => x.UserName == request.UserName, cancellationToken))
                throw new InvalidOperationException("Username already exists");

            var user = new AppUser
            {
                InstitutionId = institutionId,
                UserName = request.UserName!,
                PasswordHash = _passwordHasher.Hash(request.Password!),
                Role = UserRole.Student,
                StudentProfileId = student.Id,
                IsActive = true
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return student.Id;
    }

    public async Task<Guid> CreateParentAsync(Guid institutionId, CreateParentRequest request, CancellationToken cancellationToken = default)
    {
        if (await _db.Parents.AnyAsync(x => x.InstitutionId == institutionId && x.ParentCode == request.ParentCode, cancellationToken))
            throw new InvalidOperationException("Parent code already exists");

        var parent = new ParentProfile
        {
            InstitutionId = institutionId,
            FullName = request.FullName,
            ParentCode = request.ParentCode,
            PhoneNumber = request.PhoneNumber,
            IsActive = true
        };

        _db.Parents.Add(parent);
        await _db.SaveChangesAsync(cancellationToken);

        foreach (var studentId in request.StudentIds.Distinct())
        {
            var studentExists = await _db.Students.AnyAsync(x => x.Id == studentId && x.InstitutionId == institutionId, cancellationToken);
            if (studentExists)
            {
                _db.ParentStudentLinks.Add(new ParentStudentLink
                {
                    InstitutionId = institutionId,
                    ParentProfileId = parent.Id,
                    StudentProfileId = studentId
                });
            }
        }

        if (!string.IsNullOrWhiteSpace(request.UserName) && !string.IsNullOrWhiteSpace(request.Password))
        {
            if (await _db.Users.AnyAsync(x => x.UserName == request.UserName, cancellationToken))
                throw new InvalidOperationException("Username already exists");

            _db.Users.Add(new AppUser
            {
                InstitutionId = institutionId,
                UserName = request.UserName!,
                PasswordHash = _passwordHasher.Hash(request.Password!),
                Role = UserRole.Parent,
                ParentProfileId = parent.Id,
                IsActive = true
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return parent.Id;
    }

    public async Task<Guid> CreateExamAsync(Guid institutionId, Guid createdByUserId, CreateExamRequest request, CancellationToken cancellationToken = default)
    {
        if (request.EndAtUtc <= request.StartAtUtc)
            throw new InvalidOperationException("Exam end time must be after start time");

        if (await _db.Exams.AnyAsync(x => x.InstitutionId == institutionId && x.ExamCode == request.ExamCode, cancellationToken))
            throw new InvalidOperationException("Exam code already exists");

        var exam = new Exam
        {
            InstitutionId = institutionId,
            Title = request.Title,
            Topic = request.Topic,
            Description = request.Description,
            ExamCode = request.ExamCode,
            StartAtUtc = request.StartAtUtc,
            EndAtUtc = request.EndAtUtc,
            BankQuestionCount = request.BankQuestionCount,
            ExamQuestionCount = request.ExamQuestionCount,
            CreatedManually = request.CreatedManually,
            AllowStudentExit = request.AllowStudentExit,
            EnableAntiCheat = request.EnableAntiCheat,
            MaxViolationCount = request.MaxViolationCount <= 0 ? 3 : request.MaxViolationCount,
            CreatedByUserId = createdByUserId,
            IsPublished = true
        };

        _db.Exams.Add(exam);
        await _db.SaveChangesAsync(cancellationToken);
        return exam.Id;
    }

    public async Task<Guid> AddQuestionAsync(Guid examId, AddQuestionRequest request, CancellationToken cancellationToken = default)
    {
        var exam = await _db.Exams.FirstOrDefaultAsync(x => x.Id == examId, cancellationToken)
            ?? throw new InvalidOperationException("Exam not found");

        var question = new ExamQuestion
        {
            InstitutionId = exam.InstitutionId,
            ExamId = examId,
            QuestionText = request.QuestionText,
            ChoiceA = request.ChoiceA,
            ChoiceB = request.ChoiceB,
            ChoiceC = request.ChoiceC,
            ChoiceD = request.ChoiceD,
            CorrectAnswer = request.CorrectAnswer.ToUpperInvariant(),
            Explanation = request.Explanation
        };

        _db.Questions.Add(question);
        exam.BankQuestionCount += 1;
        await _db.SaveChangesAsync(cancellationToken);
        return question.Id;
    }

    public async Task<Guid> RegisterStudentToExamAsync(Guid institutionId, Guid examId, Guid studentId, Guid assignedByUserId, CancellationToken cancellationToken = default)
    {
        var examExists = await _db.Exams.AnyAsync(x => x.Id == examId && x.InstitutionId == institutionId, cancellationToken);
        var studentExists = await _db.Students.AnyAsync(x => x.Id == studentId && x.InstitutionId == institutionId, cancellationToken);

        if (!examExists || !studentExists)
            throw new InvalidOperationException("Exam or student not found");

        var exists = await _db.Registrations.AnyAsync(
            x => x.InstitutionId == institutionId && x.ExamId == examId && x.StudentProfileId == studentId,
            cancellationToken
        );

        if (exists)
            throw new InvalidOperationException("Student already registered on exam");

        var reg = new ExamRegistration
        {
            InstitutionId = institutionId,
            ExamId = examId,
            StudentProfileId = studentId,
            AssignedByUserId = assignedByUserId,
            IsActive = true
        };

        _db.Registrations.Add(reg);
        await _db.SaveChangesAsync(cancellationToken);
        return reg.Id;
    }
}
