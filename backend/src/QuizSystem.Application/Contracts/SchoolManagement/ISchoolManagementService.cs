using QuizSystem.Application.DTOs.SchoolManagement;

namespace QuizSystem.Application.Contracts.SchoolManagement;

public interface ISchoolManagementService
{
    Task<IReadOnlyList<GradeLevelDto>> GetGradeLevelsAsync(Guid institutionId, CancellationToken cancellationToken);
    Task<GradeLevelDto> CreateGradeLevelAsync(Guid institutionId, UpsertGradeLevelRequest request, CancellationToken cancellationToken);
    Task<GradeLevelDto> UpdateGradeLevelAsync(Guid institutionId, Guid gradeLevelId, UpsertGradeLevelRequest request, CancellationToken cancellationToken);
    Task SetGradeLevelStatusAsync(Guid institutionId, Guid gradeLevelId, bool isActive, CancellationToken cancellationToken);
    Task DeleteGradeLevelAsync(Guid institutionId, Guid gradeLevelId, CancellationToken cancellationToken);
    Task<IReadOnlyList<SubjectDto>> GetSubjectsAsync(Guid institutionId, Guid? gradeLevelId, CancellationToken cancellationToken);
    Task<SubjectDto> CreateSubjectAsync(Guid institutionId, UpsertSubjectRequest request, CancellationToken cancellationToken);
    Task<SubjectDto> UpdateSubjectAsync(Guid institutionId, Guid subjectId, UpsertSubjectRequest request, CancellationToken cancellationToken);
    Task SetSubjectStatusAsync(Guid institutionId, Guid subjectId, bool isActive, CancellationToken cancellationToken);
    Task DeleteSubjectAsync(Guid institutionId, Guid subjectId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TeacherProfileDto>> GetTeachersAsync(Guid institutionId, CancellationToken cancellationToken);
    Task<TeacherProfileDto> CreateTeacherAsync(Guid institutionId, UpsertTeacherProfileRequest request, CancellationToken cancellationToken);
    Task<TeacherProfileDto> UpdateTeacherAsync(Guid institutionId, Guid teacherId, UpsertTeacherProfileRequest request, CancellationToken cancellationToken);
    Task SetTeacherStatusAsync(Guid institutionId, Guid teacherId, bool isActive, CancellationToken cancellationToken);
    Task DeleteTeacherAsync(Guid institutionId, Guid teacherId, CancellationToken cancellationToken);
    Task AssignTeacherSubjectsAsync(Guid institutionId, Guid teacherId, AssignTeacherSubjectsRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<ClassSectionDto>> GetClassSectionsAsync(Guid institutionId, Guid? gradeLevelId, Guid? subjectId, Guid? teacherProfileId, CancellationToken cancellationToken);
    Task<ClassSectionDto> CreateClassSectionAsync(Guid institutionId, UpsertClassSectionRequest request, CancellationToken cancellationToken);
    Task<ClassSectionDto> UpdateClassSectionAsync(Guid institutionId, Guid classSectionId, UpsertClassSectionRequest request, CancellationToken cancellationToken);
    Task SetClassSectionStatusAsync(Guid institutionId, Guid classSectionId, bool isActive, CancellationToken cancellationToken);
    Task DeleteClassSectionAsync(Guid institutionId, Guid classSectionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<SectionStudentDto>> GetSectionStudentsAsync(Guid institutionId, Guid classSectionId, CancellationToken cancellationToken);
    Task AssignStudentsToSectionAsync(Guid institutionId, Guid classSectionId, AssignSectionStudentsRequest request, CancellationToken cancellationToken);
    Task RemoveStudentFromSectionAsync(Guid institutionId, Guid classSectionId, Guid studentProfileId, CancellationToken cancellationToken);
}
