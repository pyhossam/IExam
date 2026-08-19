using QuizSystem.Application.DTOs;

namespace QuizSystem.Application.Contracts.Users;

public interface IUserManagementService
{
    Task<List<UserListItemDto>> GetUsersAsync(Guid? institutionId, bool isSuperAdmin, CancellationToken cancellationToken = default);
    Task<UserListItemDto> CreateUserAsync(Guid? institutionId, bool isSuperAdmin, CreateUserManagementRequest request, CancellationToken cancellationToken = default);
    Task<UserListItemDto> UpdateUserAsync(Guid? institutionId, bool isSuperAdmin, Guid userId, UpdateUserManagementRequest request, CancellationToken cancellationToken = default);
    Task ToggleUserStatusAsync(Guid? institutionId, bool isSuperAdmin, Guid userId, bool isActive, CancellationToken cancellationToken = default);
    Task DeleteUserAsync(Guid? institutionId, bool isSuperAdmin, Guid userId, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(Guid? institutionId, bool isSuperAdmin, Guid userId, AdminResetPasswordRequest request, CancellationToken cancellationToken = default);
}
