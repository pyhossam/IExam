using Microsoft.EntityFrameworkCore;
using QuizSystem.Domain.Entities;

namespace QuizSystem.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Institution> Institutions => Set<Institution>();
    
    public DbSet<TeacherProfile> TeacherProfiles => Set<TeacherProfile>();
public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<StudentProfile> Students => Set<StudentProfile>();
    public DbSet<StudentAccountRequest> StudentAccountRequests => Set<StudentAccountRequest>();
    public DbSet<ParentProfile> Parents => Set<ParentProfile>();
    public DbSet<ParentStudentLink> ParentStudentLinks => Set<ParentStudentLink>();
    public DbSet<TeacherProfile> Teachers => Set<TeacherProfile>();
    public DbSet<TeacherSubject> TeacherSubjects => Set<TeacherSubject>();
    public DbSet<GradeLevel> GradeLevels => Set<GradeLevel>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<CourseLearningOutcome> CourseLearningOutcomes => Set<CourseLearningOutcome>();
    public DbSet<ClassSection> ClassSections => Set<ClassSection>();
    public DbSet<SectionStudent> SectionStudents => Set<SectionStudent>();
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<ExamQuestion> Questions => Set<ExamQuestion>();
    public DbSet<ExamRegistration> Registrations => Set<ExamRegistration>();
    public DbSet<ExamAttempt> Attempts => Set<ExamAttempt>();
    public DbSet<ExamAttempt> ExamAttempts => Set<ExamAttempt>();
    public DbSet<AttemptAnswer> AttemptAnswers => Set<AttemptAnswer>();
    public DbSet<AttemptAnswer> ExamAttemptAnswers => Set<AttemptAnswer>();
    public DbSet<ExamAttemptDraftAnswer> ExamAttemptDraftAnswers => Set<ExamAttemptDraftAnswer>();
    public DbSet<ExamAttemptViolation> ExamAttemptViolations => Set<ExamAttemptViolation>();
    public DbSet<ExamAttemptQuestionSnapshot> ExamAttemptQuestionSnapshots => Set<ExamAttemptQuestionSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ExamAttempt>().ToTable("Attempts");
        modelBuilder.Entity<AttemptAnswer>().ToTable("AttemptAnswers");

        modelBuilder.Entity<AppUser>().HasIndex(x => x.UserName).IsUnique();
        modelBuilder.Entity<AppUser>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<StudentAccountRequest>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<StudentAccountRequest>().HasIndex(x => new { x.InstitutionId, x.Status });
        modelBuilder.Entity<Institution>().HasIndex(x => x.Name);

        modelBuilder.Entity<StudentProfile>()
            .HasIndex(x => new { x.InstitutionId, x.StudentCode })
            .IsUnique();

        modelBuilder.Entity<ParentProfile>()
            .HasIndex(x => new { x.InstitutionId, x.ParentCode })
            .IsUnique();

        modelBuilder.Entity<TeacherProfile>()
            .HasIndex(x => new { x.InstitutionId, x.TeacherCode })
            .IsUnique();

        modelBuilder.Entity<Exam>()
            .HasIndex(x => new { x.InstitutionId, x.ExamCode })
            .IsUnique();

        modelBuilder.Entity<Subject>()
            .HasIndex(x => new { x.InstitutionId, x.Code })
            .IsUnique();

        modelBuilder.Entity<ExamRegistration>()
            .HasIndex(x => new { x.InstitutionId, x.ExamId, x.StudentProfileId })
            .IsUnique();

        modelBuilder.Entity<ExamAttempt>()
            .HasIndex(x => new { x.InstitutionId, x.ExamId, x.StudentProfileId, x.AttemptNumber })
            .IsUnique();

        modelBuilder.Entity<CourseLearningOutcome>()
            .HasIndex(x => new { x.InstitutionId, x.SubjectId, x.Code })
            .IsUnique();

        modelBuilder.Entity<CourseLearningOutcome>()
            .HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ExamQuestion>()
            .HasOne(x => x.CourseLearningOutcome).WithMany()
            .HasForeignKey(x => x.CourseLearningOutcomeId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ParentStudentLink>()
            .HasIndex(x => new { x.InstitutionId, x.ParentProfileId, x.StudentProfileId })
            .IsUnique();

        modelBuilder.Entity<SectionStudent>()
            .HasIndex(x => new { x.InstitutionId, x.ClassSectionId, x.StudentProfileId })
            .IsUnique();

        modelBuilder.Entity<TeacherSubject>()
            .HasIndex(x => new { x.InstitutionId, x.TeacherProfileId, x.SubjectId })
            .IsUnique();

        
        modelBuilder.Entity<AppUser>().HasIndex(x => x.InstitutionId);
        modelBuilder.Entity<StudentProfile>().HasIndex(x => x.InstitutionId);
        modelBuilder.Entity<ParentProfile>().HasIndex(x => x.InstitutionId);
        modelBuilder.Entity<Exam>().HasIndex(x => x.InstitutionId);
        modelBuilder.Entity<ExamQuestion>().HasIndex(x => x.InstitutionId);
        modelBuilder.Entity<ExamRegistration>().HasIndex(x => x.InstitutionId);
        modelBuilder.Entity<ExamAttempt>().HasIndex(x => x.InstitutionId);
        modelBuilder.Entity<TeacherSubject>().HasIndex(x => x.InstitutionId);
        modelBuilder.Entity<CourseLearningOutcome>().HasIndex(x => x.InstitutionId);

        modelBuilder.Entity<AppUser>()
            .HasOne(x => x.Institution)
            .WithMany(x => x.Users)
            .HasForeignKey(x => x.InstitutionId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AppUser>()
            .HasOne(x => x.StudentProfile)
            .WithMany()
            .HasForeignKey(x => x.StudentProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AppUser>()
            .HasOne(x => x.ParentProfile)
            .WithOne(x => x.User)
            .HasForeignKey<AppUser>(x => x.ParentProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AppUser>()
            .HasOne(x => x.TeacherProfile)
            .WithOne(x => x.User)
            .HasForeignKey<AppUser>(x => x.TeacherProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TeacherSubject>()
            .HasOne(x => x.Institution)
            .WithMany()
            .HasForeignKey(x => x.InstitutionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TeacherSubject>()
            .HasOne(x => x.TeacherProfile)
            .WithMany(x => x.TeacherSubjects)
            .HasForeignKey(x => x.TeacherProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TeacherSubject>()
            .HasOne(x => x.Subject)
            .WithMany(x => x.TeacherSubjects)
            .HasForeignKey(x => x.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
