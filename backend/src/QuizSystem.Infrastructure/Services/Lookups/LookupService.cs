using Microsoft.EntityFrameworkCore;
using QuizSystem.Application.Contracts.Lookups;
using QuizSystem.Application.DTOs;
using QuizSystem.Domain.Entities;
using QuizSystem.Infrastructure.Persistence;

namespace QuizSystem.Infrastructure.Services.Lookups;

public class LookupService : ILookupService
{
    private readonly AppDbContext _db;

    public LookupService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<LookupItemDto>> GetStudentsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Students
            .OrderBy(x => x.FullName)
            .Select(x => new LookupItemDto
            {
                Id = x.Id,
                Name = x.FullName,
                Code = x.StudentCode,
                Grade=x.Grade

            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<LookupItemDto>> GetParentsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Parents
            .OrderBy(x => x.FullName)
            .Select(x => new LookupItemDto
            {
                Id = x.Id,
                Name = x.FullName,
                Code = x.ParentCode
            })
            .ToListAsync(cancellationToken);
    }
    public async Task<List<ParentLookupResponse>> GetParentLookupsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Set<ParentProfile>()
            .AsNoTracking()
            .Include(p => p.ParentStudentLinks)
            .Include(p => p.User)
            .Select(p => new ParentLookupResponse
            {
                Id = p.Id,
                Name = p.FullName,
                Code = p.ParentCode,
                PhoneNumber = p.PhoneNumber,
                UserName = p.User != null ? p.User.UserName : null,
                ChildrenCount = p.ParentStudentLinks.Count
            })
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }
}
