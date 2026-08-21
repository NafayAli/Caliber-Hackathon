using Caliber.Api.Abstractions;
using Caliber.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Caliber.Api.Data;

public class CaliberDbContext : DbContext
{
    private readonly ICurrentUser? _currentUser;
    private readonly IClock _clock;

    public CaliberDbContext(DbContextOptions<CaliberDbContext> options, IClock clock, ICurrentUser? currentUser = null)
        : base(options)
    {
        _clock = clock;
        _currentUser = currentUser;
    }

    public DbSet<Location> Locations => Set<Location>();

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<JobRole> JobRoles => Set<JobRole>();

    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<Certification> Certifications => Set<Certification>();

    public DbSet<CertificationSkill> CertificationSkills => Set<CertificationSkill>();

    public DbSet<EmployeeCertification> EmployeeCertifications => Set<EmployeeCertification>();

    public DbSet<CertificationAward> CertificationAwards => Set<CertificationAward>();

    public DbSet<TrainingProgram> TrainingPrograms => Set<TrainingProgram>();

    public DbSet<TrainingModule> TrainingModules => Set<TrainingModule>();

    public DbSet<TrainingProgramSkill> TrainingProgramSkills => Set<TrainingProgramSkill>();

    public DbSet<EmployeeTraining> EmployeeTrainings => Set<EmployeeTraining>();

    public DbSet<Skill> Skills => Set<Skill>();

    public DbSet<EmployeeSkill> EmployeeSkills => Set<EmployeeSkill>();

    public DbSet<RoleRequirement> RoleRequirements => Set<RoleRequirement>();

    public DbSet<Evidence> Evidence => Set<Evidence>();

    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

    public DbSet<AppSettings> AppSettings => Set<AppSettings>();

    public DbSet<RoleModuleAccess> RoleModuleAccess => Set<RoleModuleAccess>();

    public DbSet<SkillAssignmentRequest> SkillAssignmentRequests => Set<SkillAssignmentRequest>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<RenewalRequest> RenewalRequests => Set<RenewalRequest>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        ConfigureOrganisation(b);
        ConfigureSettings(b);
        ConfigureNotifications(b);
        ConfigureCertifications(b);
        ConfigureTraining(b);
        ConfigureSkills(b);
        ConfigureRequirementsAndEvidence(b);
        ConfigureAuditing(b);
    }

    private static void ConfigureOrganisation(ModelBuilder b)
    {
        b.Entity<Location>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Code).HasMaxLength(20).IsRequired();
            e.Property(x => x.City).HasMaxLength(100);
            e.HasIndex(x => x.Code).IsUnique();
        });

        b.Entity<Department>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
        });

        b.Entity<JobRole>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
            e.HasOne(x => x.Department)
                .WithMany(d => d.JobRoles)
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Employee>(e =>
        {
            e.Property(x => x.FirstName).HasMaxLength(60).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(60).IsRequired();
            e.Property(x => x.Email).HasMaxLength(200).IsRequired();
            e.Property(x => x.ExternalEmployeeNo).HasMaxLength(30);
            e.Ignore(x => x.FullName);

            e.HasIndex(x => x.Email).IsUnique();

            // The employee list filters by location and role on every request.
            e.HasIndex(x => new { x.LocationId, x.JobRoleId, x.IsActive });

            e.HasOne(x => x.JobRole)
                .WithMany(r => r.Employees)
                .HasForeignKey(x => x.JobRoleId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Location)
                .WithMany(l => l.Employees)
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<UserAccount>(e =>
        {
            e.Property(x => x.Email).HasMaxLength(200).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.EmployeeId).IsUnique();

            e.HasOne(x => x.Employee)
                .WithOne(emp => emp.UserAccount)
                .HasForeignKey<UserAccount>(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureCertifications(ModelBuilder b)
    {
        b.Entity<Certification>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.Code).HasMaxLength(30).IsRequired();
            e.Property(x => x.IssuingBody).HasMaxLength(120).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000);
            e.HasIndex(x => x.Code).IsUnique();
        });

        b.Entity<CertificationSkill>(e =>
        {
            e.HasKey(x => new { x.CertificationId, x.SkillId });

            e.HasOne(x => x.Certification)
                .WithMany(c => c.GrantedSkills)
                .HasForeignKey(x => x.CertificationId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Skill)
                .WithMany(s => s.GrantedByCertifications)
                .HasForeignKey(x => x.SkillId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<EmployeeCertification>(e =>
        {
            e.Property(x => x.Notes).HasMaxLength(1000);

            // An employee owes a given certification at most once.
            e.HasIndex(x => new { x.EmployeeId, x.CertificationId }).IsUnique();

            // Covering index for the per-employee readiness rollup.
            e.HasIndex(x => new { x.EmployeeId, x.Status });

            e.HasOne(x => x.Employee)
                .WithMany(emp => emp.Certifications)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Certification)
                .WithMany(c => c.Assignments)
                .HasForeignKey(x => x.CertificationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<CertificationAward>(e =>
        {
            e.Property(x => x.CertificateNumber).HasMaxLength(80);
            e.Property(x => x.RecordedBy).HasMaxLength(120).IsRequired();
            e.Property(x => x.Notes).HasMaxLength(1000);

            // Resolving "the latest award" is the hottest lookup in the schema.
            e.HasIndex(x => new { x.EmployeeCertificationId, x.AwardedOn })
                .IsDescending(false, true);

            // Drives the expiring-soon window across the whole workforce.
            e.HasIndex(x => x.ExpiresOn);

            e.HasOne(x => x.EmployeeCertification)
                .WithMany(ec => ec.Awards)
                .HasForeignKey(x => x.EmployeeCertificationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureTraining(ModelBuilder b)
    {
        b.Entity<TrainingProgram>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.Code).HasMaxLength(30).IsRequired();
            e.Property(x => x.Provider).HasMaxLength(120).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.EstimatedDurationHours).HasPrecision(5, 2);
            e.HasIndex(x => x.Code).IsUnique();
        });

        b.Entity<TrainingModule>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.EstimatedDurationHours).HasPrecision(5, 2);

            e.HasOne(x => x.TrainingProgram)
                .WithMany(p => p.Modules)
                .HasForeignKey(x => x.TrainingProgramId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<TrainingProgramSkill>(e =>
        {
            e.HasKey(x => new { x.TrainingProgramId, x.SkillId });

            e.HasOne(x => x.TrainingProgram)
                .WithMany(p => p.GrantedSkills)
                .HasForeignKey(x => x.TrainingProgramId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Skill)
                .WithMany(s => s.GrantedByTraining)
                .HasForeignKey(x => x.SkillId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<EmployeeTraining>(e =>
        {
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.Property(x => x.AcknowledgedBy).HasMaxLength(120);

            e.HasIndex(x => new { x.EmployeeId, x.TrainingProgramId }).IsUnique();
            e.HasIndex(x => new { x.EmployeeId, x.Status });

            // Drives the expiring-soon window for recurring training.
            e.HasIndex(x => x.NextDueOn);

            e.HasOne(x => x.Employee)
                .WithMany(emp => emp.Trainings)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.TrainingProgram)
                .WithMany(p => p.Assignments)
                .HasForeignKey(x => x.TrainingProgramId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureSkills(ModelBuilder b)
    {
        b.Entity<Skill>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000);
            e.HasIndex(x => x.Name).IsUnique();
        });

        b.Entity<EmployeeSkill>(e =>
        {
            e.Property(x => x.AssessedBy).HasMaxLength(120).IsRequired();
            e.Property(x => x.Notes).HasMaxLength(1000);

            e.HasIndex(x => new { x.EmployeeId, x.SkillId }).IsUnique();
            e.HasIndex(x => new { x.SkillId, x.ProficiencyLevel });

            e.HasOne(x => x.Employee)
                .WithMany(emp => emp.Skills)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Skill)
                .WithMany(s => s.EmployeeSkills)
                .HasForeignKey(x => x.SkillId)
                .OnDelete(DeleteBehavior.Restrict);

            // Provenance only - deleting a catalog entry must not delete the skill record.
            e.HasOne(x => x.SourceCertification)
                .WithMany()
                .HasForeignKey(x => x.SourceCertificationId)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(x => x.SourceTrainingProgram)
                .WithMany()
                .HasForeignKey(x => x.SourceTrainingProgramId)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(x => x.SourceEmployeeCertification)
                .WithMany()
                .HasForeignKey(x => x.SourceEmployeeCertificationId)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(x => x.SourceEmployeeTraining)
                .WithMany()
                .HasForeignKey(x => x.SourceEmployeeTrainingId)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasIndex(x => x.ExpiresOn);
            e.HasIndex(x => x.Status);
        });
    }

    private static void ConfigureSettings(ModelBuilder b)
    {
        b.Entity<AppSettings>(e =>
        {
            e.Property(x => x.ApplicationName).HasMaxLength(120).IsRequired();
            e.Property(x => x.OrganizationName).HasMaxLength(200).IsRequired();
            e.Property(x => x.ContactEmail).HasMaxLength(200);
            e.Property(x => x.SupportPhone).HasMaxLength(40);
            e.Property(x => x.Tagline).HasMaxLength(200);
            e.Property(x => x.SidebarThemeKey).HasMaxLength(32).IsRequired();
        });

        b.Entity<RoleModuleAccess>(e =>
        {
            e.Property(x => x.ModuleKey).HasMaxLength(40).IsRequired();
            e.HasIndex(x => new { x.AccessLevel, x.ModuleKey }).IsUnique();
        });

        b.Entity<SkillAssignmentRequest>(e =>
        {
            e.Property(x => x.ReviewNotes).HasMaxLength(1000);
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.HasIndex(x => new { x.EmployeeId, x.SkillId, x.Status });

            e.HasOne(x => x.Employee)
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Skill)
                .WithMany()
                .HasForeignKey(x => x.SkillId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.RequestedBy)
                .WithMany()
                .HasForeignKey(x => x.RequestedByEmployeeId)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(x => x.ReviewedBy)
                .WithMany()
                .HasForeignKey(x => x.ReviewedByEmployeeId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureNotifications(ModelBuilder b)
    {
        b.Entity<Notification>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Message).HasMaxLength(2000).IsRequired();
            e.HasIndex(x => new { x.RecipientEmployeeId, x.IsRead, x.CreatedAt });

            e.HasOne(x => x.Recipient)
                .WithMany()
                .HasForeignKey(x => x.RecipientEmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.CreatedBy)
                .WithMany()
                .HasForeignKey(x => x.CreatedByEmployeeId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<RenewalRequest>(e =>
        {
            e.Property(x => x.EmployeeNote).HasMaxLength(1000);
            e.Property(x => x.ReviewerNote).HasMaxLength(1000);
            e.HasIndex(x => new { x.EmployeeId, x.Status });
            e.HasIndex(x => new { x.Kind, x.AssignmentId, x.Status });

            e.HasOne(x => x.Employee)
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.ReviewedBy)
                .WithMany()
                .HasForeignKey(x => x.ReviewedByEmployeeId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureRequirementsAndEvidence(ModelBuilder b)
    {
        b.Entity<RoleRequirement>(e =>
        {
            e.HasIndex(x => new { x.JobRoleId, x.Kind });

            e.HasOne(x => x.JobRole)
                .WithMany(r => r.Requirements)
                .HasForeignKey(x => x.JobRoleId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Certification)
                .WithMany()
                .HasForeignKey(x => x.CertificationId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.TrainingProgram)
                .WithMany()
                .HasForeignKey(x => x.TrainingProgramId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Skill)
                .WithMany()
                .HasForeignKey(x => x.SkillId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Evidence>(e =>
        {
            e.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            e.Property(x => x.StoredFileName).HasMaxLength(100).IsRequired();
            e.Property(x => x.ContentType).HasMaxLength(120).IsRequired();
            e.Property(x => x.UploadedBy).HasMaxLength(120).IsRequired();
            e.Property(x => x.VerifiedBy).HasMaxLength(120);

            e.HasIndex(x => x.EmployeeId);

            e.HasOne(x => x.Employee)
                .WithMany(emp => emp.Evidence)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // NoAction on the optional links: SQL Server rejects multiple cascade
            // paths to the same table, and the Employee cascade above already
            // covers cleanup.
            e.HasOne(x => x.EmployeeCertification)
                .WithMany(ec => ec.Evidence)
                .HasForeignKey(x => x.EmployeeCertificationId)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(x => x.EmployeeTraining)
                .WithMany(et => et.Evidence)
                .HasForeignKey(x => x.EmployeeTrainingId)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(x => x.EmployeeSkill)
                .WithMany(es => es.Evidence)
                .HasForeignKey(x => x.EmployeeSkillId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureAuditing(ModelBuilder b)
    {
        foreach (var entityType in b.Model.GetEntityTypes()
                     .Where(t => typeof(AuditableEntity).IsAssignableFrom(t.ClrType)))
        {
            b.Entity(entityType.ClrType).Property(nameof(AuditableEntity.CreatedBy)).HasMaxLength(120);
            b.Entity(entityType.ClrType).Property(nameof(AuditableEntity.ModifiedBy)).HasMaxLength(120);
            b.Entity(entityType.ClrType).Property(nameof(AuditableEntity.RowVersion)).IsRowVersion();
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampAudit();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        StampAudit();
        return base.SaveChanges();
    }

    private void StampAudit()
    {
        var actor = _currentUser?.IsAuthenticated == true ? _currentUser.DisplayName : "system";
        var now = _clock.Now;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = actor;
                    break;
                case EntityState.Modified:
                    entry.Entity.ModifiedAt = now;
                    entry.Entity.ModifiedBy = actor;
                    break;
            }
        }
    }
}
