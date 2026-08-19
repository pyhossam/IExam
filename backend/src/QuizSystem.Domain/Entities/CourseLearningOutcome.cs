using QuizSystem.Domain.Common;
using QuizSystem.Domain.Enums;

namespace QuizSystem.Domain.Entities;

public class CourseLearningOutcome : BaseEntity
{
    public Guid InstitutionId { get; set; }
    public Institution Institution { get; set; } = default!;
    public Guid SubjectId { get; set; }
    public Subject Subject { get; set; } = default!;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CloDomain Domain { get; set; } = CloDomain.Knowledge;
    public CognitiveLevel CognitiveLevel { get; set; } = CognitiveLevel.Understand;
    public decimal TargetPercentage { get; set; } = 70;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
