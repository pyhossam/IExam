using QuizSystem.Application.DTOs;

namespace QuizSystem.Application.Contracts.Lookups;

public interface ILookupService
{
    Task<List<LookupItemDto>> GetStudentsAsync(CancellationToken cancellationToken = default);
    Task<List<LookupItemDto>> GetParentsAsync(CancellationToken cancellationToken = default);
    Task<List<ParentLookupResponse>> GetParentLookupsAsync(CancellationToken cancellationToken = default);
}
