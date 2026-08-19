using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizSystem.Api.Infrastructure.Tenant;
using QuizSystem.Application.Contracts.SchoolManagement;
using QuizSystem.Application.DTOs.SchoolManagement;
using QuizSystem.Infrastructure.Persistence;

namespace QuizSystem.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/school")]
[Authorize(Policy = "AdminOnly")]
public class SchoolManagementController : ControllerBase
{
    private readonly ISchoolManagementService _service;
    private readonly AppDbContext _db;
    public SchoolManagementController(ISchoolManagementService service, AppDbContext db) { _service = service; _db = db; }
    private Task<Guid> InstitutionId(CancellationToken ct) => TenantResolver.RequireCurrentInstitutionIdAsync(_db, User, ct);

    [HttpGet("grade-levels")] public async Task<IActionResult> GetGradeLevels(CancellationToken ct) => Ok(await _service.GetGradeLevelsAsync(await InstitutionId(ct), ct));
    [HttpPost("grade-levels")] public async Task<IActionResult> CreateGradeLevel([FromBody] UpsertGradeLevelRequest request, CancellationToken ct) => Ok(await _service.CreateGradeLevelAsync(await InstitutionId(ct), request, ct));
    [HttpPut("grade-levels/{id:guid}")] public async Task<IActionResult> UpdateGradeLevel(Guid id, [FromBody] UpsertGradeLevelRequest request, CancellationToken ct) => Ok(await _service.UpdateGradeLevelAsync(await InstitutionId(ct), id, request, ct));
    [HttpPatch("grade-levels/{id:guid}/status")] public async Task<IActionResult> SetGradeLevelStatus(Guid id, [FromQuery] bool isActive, CancellationToken ct) { await _service.SetGradeLevelStatusAsync(await InstitutionId(ct), id, isActive, ct); return NoContent(); }
    [HttpDelete("grade-levels/{id:guid}")] public async Task<IActionResult> DeleteGradeLevel(Guid id, CancellationToken ct) { await _service.DeleteGradeLevelAsync(await InstitutionId(ct), id, ct); return NoContent(); }

    [HttpGet("subjects")] public async Task<IActionResult> GetSubjects([FromQuery] Guid? gradeLevelId, CancellationToken ct) => Ok(await _service.GetSubjectsAsync(await InstitutionId(ct), gradeLevelId, ct));
    [HttpPost("subjects")] public async Task<IActionResult> CreateSubject([FromBody] UpsertSubjectRequest request, CancellationToken ct) => Ok(await _service.CreateSubjectAsync(await InstitutionId(ct), request, ct));
    [HttpPut("subjects/{id:guid}")] public async Task<IActionResult> UpdateSubject(Guid id, [FromBody] UpsertSubjectRequest request, CancellationToken ct) => Ok(await _service.UpdateSubjectAsync(await InstitutionId(ct), id, request, ct));
    [HttpPatch("subjects/{id:guid}/status")] public async Task<IActionResult> SetSubjectStatus(Guid id, [FromQuery] bool isActive, CancellationToken ct) { await _service.SetSubjectStatusAsync(await InstitutionId(ct), id, isActive, ct); return NoContent(); }
    [HttpDelete("subjects/{id:guid}")] public async Task<IActionResult> DeleteSubject(Guid id, CancellationToken ct) { await _service.DeleteSubjectAsync(await InstitutionId(ct), id, ct); return NoContent(); }

    [HttpGet("teachers")] public async Task<IActionResult> GetTeachers(CancellationToken ct) => Ok(await _service.GetTeachersAsync(await InstitutionId(ct), ct));
    [HttpPost("teachers")] public async Task<IActionResult> CreateTeacher([FromBody] UpsertTeacherProfileRequest request, CancellationToken ct) => Ok(await _service.CreateTeacherAsync(await InstitutionId(ct), request, ct));
    [HttpPut("teachers/{id:guid}")] public async Task<IActionResult> UpdateTeacher(Guid id, [FromBody] UpsertTeacherProfileRequest request, CancellationToken ct) => Ok(await _service.UpdateTeacherAsync(await InstitutionId(ct), id, request, ct));
    [HttpPatch("teachers/{id:guid}/status")] public async Task<IActionResult> SetTeacherStatus(Guid id, [FromQuery] bool isActive, CancellationToken ct) { await _service.SetTeacherStatusAsync(await InstitutionId(ct), id, isActive, ct); return NoContent(); }
    [HttpDelete("teachers/{id:guid}")] public async Task<IActionResult> DeleteTeacher(Guid id, CancellationToken ct) { await _service.DeleteTeacherAsync(await InstitutionId(ct), id, ct); return NoContent(); }
    [HttpPost("teachers/{id:guid}/subjects")] public async Task<IActionResult> AssignTeacherSubjects(Guid id, [FromBody] AssignTeacherSubjectsRequest request, CancellationToken ct) { await _service.AssignTeacherSubjectsAsync(await InstitutionId(ct), id, request, ct); return NoContent(); }

    [HttpGet("class-sections")] public async Task<IActionResult> GetClassSections([FromQuery] Guid? gradeLevelId, [FromQuery] Guid? subjectId, [FromQuery] Guid? teacherProfileId, CancellationToken ct) => Ok(await _service.GetClassSectionsAsync(await InstitutionId(ct), gradeLevelId, subjectId, teacherProfileId, ct));
    [HttpPost("class-sections")] public async Task<IActionResult> CreateClassSection([FromBody] UpsertClassSectionRequest request, CancellationToken ct) => Ok(await _service.CreateClassSectionAsync(await InstitutionId(ct), request, ct));
    [HttpPut("class-sections/{id:guid}")] public async Task<IActionResult> UpdateClassSection(Guid id, [FromBody] UpsertClassSectionRequest request, CancellationToken ct) => Ok(await _service.UpdateClassSectionAsync(await InstitutionId(ct), id, request, ct));
    [HttpPatch("class-sections/{id:guid}/status")] public async Task<IActionResult> SetClassSectionStatus(Guid id, [FromQuery] bool isActive, CancellationToken ct) { await _service.SetClassSectionStatusAsync(await InstitutionId(ct), id, isActive, ct); return NoContent(); }
    [HttpDelete("class-sections/{id:guid}")] public async Task<IActionResult> DeleteClassSection(Guid id, CancellationToken ct) { await _service.DeleteClassSectionAsync(await InstitutionId(ct), id, ct); return NoContent(); }
    [HttpGet("class-sections/{id:guid}/students")] public async Task<IActionResult> GetSectionStudents(Guid id, CancellationToken ct) => Ok(await _service.GetSectionStudentsAsync(await InstitutionId(ct), id, ct));
    [HttpPost("class-sections/{id:guid}/students")] public async Task<IActionResult> AssignStudentsToSection(Guid id, [FromBody] AssignSectionStudentsRequest request, CancellationToken ct) { await _service.AssignStudentsToSectionAsync(await InstitutionId(ct), id, request, ct); return NoContent(); }
    [HttpDelete("class-sections/{id:guid}/students/{studentProfileId:guid}")] public async Task<IActionResult> RemoveStudentFromSection(Guid id, Guid studentProfileId, CancellationToken ct) { await _service.RemoveStudentFromSectionAsync(await InstitutionId(ct), id, studentProfileId, ct); return NoContent(); }
}
