using QuizSystem.Application.DTOs;

namespace QuizSystem.Application.Contracts.Portals;
public interface IPortalService
{
    Task<StudentPortalDashboardDto> GetStudentDashboardAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<ParentPortalDashboardDto> GetParentDashboardAsync(Guid parentId, CancellationToken cancellationToken = default);
    Task<List<LeaderboardItemDto>> GetExamLeaderboardAsync(Guid examId, CancellationToken cancellationToken = default);
}
