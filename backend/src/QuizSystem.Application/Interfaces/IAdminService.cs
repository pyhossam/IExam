using QuizSystem.Application.DTOs;

namespace QuizSystem.Application.Interfaces;
public interface IAdminService
{
    Task<DashboardResponse> GetDashboardAsync(Guid institutionId, CancellationToken cancellationToken = default);
    Task<Guid> CreateUserAsync(Guid institutionId, CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<Guid> CreateStudentAsync(Guid institutionId, CreateStudentRequest request, CancellationToken cancellationToken = default);
    Task<Guid> CreateParentAsync(Guid institutionId, CreateParentRequest request, CancellationToken cancellationToken = default);
    Task<Guid> CreateExamAsync(Guid institutionId, Guid createdByUserId, CreateExamRequest request, CancellationToken cancellationToken = default);
    Task<Guid> AddQuestionAsync(Guid examId, AddQuestionRequest request, CancellationToken cancellationToken = default);
    Task<Guid> RegisterStudentToExamAsync(Guid institutionId, Guid examId, Guid studentId, Guid assignedByUserId, CancellationToken cancellationToken = default);
}
