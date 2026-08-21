using Caliber.Api.Domain;
using Caliber.Api.Security;
using Caliber.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Caliber.Api.Data;

public static class SeedData
{
    /// <summary>Canonical admin login documented in README — used to detect stale demo databases.</summary>
    public const string CanonicalAdminEmail = "marcus.chen@caliber-demo.com";

    public static async Task EnsureSeededAsync(CaliberDbContext db, CancellationToken cancellationToken = default)
    {
        if (!await db.Employees.AnyAsync(cancellationToken))
        {
            await SeedEmployeesAsync(db, cancellationToken);
        }

        await EnsureUserAccountsAsync(db, cancellationToken);
        await EnsureAppSettingsAsync(db, cancellationToken);
    }

    private static async Task EnsureAppSettingsAsync(CaliberDbContext db, CancellationToken cancellationToken)
    {
        if (!await db.AppSettings.AnyAsync(cancellationToken))
        {
            db.AppSettings.Add(new AppSettings());
        }

        await db.SaveChangesAsync(cancellationToken);

        var settingsService = new SettingsService(db, new SeedCurrentUser());
        await settingsService.EnsureModuleAccessSeededAsync(cancellationToken);
    }

    private sealed class SeedCurrentUser : Abstractions.ICurrentUser
    {
        public bool IsAuthenticated => true;
        public int EmployeeId => 0;
        public string DisplayName => "seed";
        public AccessLevel AccessLevel => AccessLevel.Admin;
        public int LocationId => 1;

        public void EnsureCanAccessEmployee(int employeeId, int locationId) { }
    }

    private static async Task EnsureUserAccountsAsync(CaliberDbContext db, CancellationToken cancellationToken)
    {
        var employees = await db.Employees.AsNoTracking().Where(e => e.IsActive).ToListAsync(cancellationToken);
        if (employees.Count == 0)
        {
            return;
        }

        var existingEmployeeIds = await db.UserAccounts
            .AsNoTracking()
            .Select(u => u.EmployeeId)
            .ToListAsync(cancellationToken);

        var missing = employees.Where(e => !existingEmployeeIds.Contains(e.Id)).ToList();
        if (missing.Count == 0)
        {
            return;
        }

        var now = DateTimeOffset.Now;
        var passwordHash = AuthService.HashPassword(AuthConstants.MasterPassword);

        foreach (var employee in missing)
        {
            db.UserAccounts.Add(new UserAccount
            {
                EmployeeId = employee.Id,
                Email = employee.Email.Trim().ToLowerInvariant(),
                PasswordHash = passwordHash,
                CreatedAt = now,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedEmployeesAsync(CaliberDbContext db, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var now = DateTimeOffset.Now;
        const string recordedBy = "seed";

        var springfield = new Location { Name = "Springfield HQ", Code = "SPR", City = "Springfield, IL" };
        var desMoines = new Location { Name = "Des Moines Branch", Code = "DSM", City = "Des Moines, IA" };
        var omaha = new Location { Name = "Omaha Branch", Code = "OMA", City = "Omaha, NE" };
        db.Locations.AddRange(springfield, desMoines, omaha);

        var serviceDept = new Department { Name = "Service" };
        var partsDept = new Department { Name = "Parts" };
        var salesDept = new Department { Name = "Sales" };
        var adminDept = new Department { Name = "Administration" };
        db.Departments.AddRange(serviceDept, partsDept, salesDept, adminDept);

        var agTechRole = new JobRole { Name = "Ag Service Technician", Department = serviceDept };
        var dieselTechRole = new JobRole { Name = "Diesel Technician", Department = serviceDept };
        var partsRole = new JobRole { Name = "Parts Specialist", Department = partsDept };
        var salesRole = new JobRole { Name = "Equipment Sales Consultant", Department = salesDept };
        var serviceManagerRole = new JobRole { Name = "Service Manager", Department = serviceDept };
        db.JobRoles.AddRange(agTechRole, dieselTechRole, partsRole, salesRole, serviceManagerRole);

        var jdAgt1 = new Certification
        {
            Name = "John Deere Ag Tech Level 1",
            Code = "JD-AGT1",
            Category = CertificationCategory.Oem,
            IssuingBody = "John Deere",
            Description = "Foundational John Deere agricultural equipment service credential.",
            ValidityMonths = 36,
            ExpiryWarningDays = 60,
            RequiresEvidence = true,
        };

        var jdAgt2 = new Certification
        {
            Name = "John Deere Ag Tech Level 2",
            Code = "JD-AGT2",
            Category = CertificationCategory.Oem,
            IssuingBody = "John Deere",
            Description = "Advanced John Deere combine and tractor diagnostics.",
            ValidityMonths = 24,
            ExpiryWarningDays = 60,
            RequiresEvidence = true,
        };

        var jdPrecision = new Certification
        {
            Name = "John Deere Precision Ag Specialist",
            Code = "JD-PRE",
            Category = CertificationCategory.Oem,
            IssuingBody = "John Deere",
            Description = "Guidance, telematics, and precision farming systems.",
            ValidityMonths = 12,
            ExpiryWarningDays = 60,
            RequiresEvidence = true,
        };

        var kubotaSvc = new Certification
        {
            Name = "Kubota Service Technician",
            Code = "KUB-SVC",
            Category = CertificationCategory.Oem,
            IssuingBody = "Kubota",
            ValidityMonths = 24,
            ExpiryWarningDays = 60,
            RequiresEvidence = true,
        };

        var osha10 = new Certification
        {
            Name = "OSHA 10-Hour Construction Safety",
            Code = "OSHA-10",
            Category = CertificationCategory.Safety,
            IssuingBody = "OSHA",
            ValidityMonths = null,
            ExpiryWarningDays = 60,
            RequiresEvidence = false,
        };

        var epa608 = new Certification
        {
            Name = "EPA Section 608 Refrigerant Handling",
            Code = "EPA-608",
            Category = CertificationCategory.Regulatory,
            IssuingBody = "EPA",
            ValidityMonths = 60,
            ExpiryWarningDays = 90,
            RequiresEvidence = true,
        };

        db.Certifications.AddRange(jdAgt1, jdAgt2, jdPrecision, kubotaSvc, osha10, epa608);

        var oshaRefresher = new TrainingProgram
        {
            Name = "Annual OSHA Safety Refresher",
            Code = "OSHA-REF",
            Category = TrainingCategory.Safety,
            Provider = "OSHA",
            DeliveryMode = DeliveryMode.Online,
            EstimatedDurationHours = 2,
            RequiresAcknowledgement = true,
            RecurrenceMonths = 12,
            ExpiryWarningDays = 30,
        };

        var jdOnboarding = new TrainingProgram
        {
            Name = "John Deere New Dealer Onboarding",
            Code = "JD-ONBD",
            Category = TrainingCategory.Onboarding,
            Provider = "John Deere",
            DeliveryMode = DeliveryMode.Online,
            EstimatedDurationHours = 8,
            RequiresAcknowledgement = true,
        };

        var kubotaProduct = new TrainingProgram
        {
            Name = "Kubota Product Knowledge",
            Code = "KUB-PROD",
            Category = TrainingCategory.Product,
            Provider = "Kubota",
            DeliveryMode = DeliveryMode.InPerson,
            EstimatedDurationHours = 6,
            RequiresAcknowledgement = false,
        };

        var forklift = new TrainingProgram
        {
            Name = "Internal Forklift Operation",
            Code = "INT-LIFT",
            Category = TrainingCategory.Internal,
            Provider = "Internal",
            DeliveryMode = DeliveryMode.InPerson,
            EstimatedDurationHours = 4,
            RequiresAcknowledgement = true,
        };

        var epaSpill = new TrainingProgram
        {
            Name = "EPA Spill Prevention (SPCC)",
            Code = "EPA-SPILL",
            Category = TrainingCategory.Safety,
            Provider = "EPA",
            DeliveryMode = DeliveryMode.Document,
            EstimatedDurationHours = 1.5m,
            RequiresAcknowledgement = true,
            RecurrenceMonths = 24,
            ExpiryWarningDays = 60,
        };

        db.TrainingPrograms.AddRange(oshaRefresher, jdOnboarding, kubotaProduct, forklift, epaSpill);

        var combineDiag = new Skill { Name = "Combine Diagnostics", Category = SkillCategory.EquipmentType };
        var dieselRepair = new Skill { Name = "Diesel Engine Repair", Category = SkillCategory.SystemType };
        var precisionAg = new Skill { Name = "Precision Ag Technology", Category = SkillCategory.Oem };
        var hydraulics = new Skill { Name = "Hydraulic Systems", Category = SkillCategory.SystemType };
        var refrigerant = new Skill { Name = "Refrigerant Handling", Category = SkillCategory.Safety };
        var forkliftSkill = new Skill { Name = "Forklift Operation", Category = SkillCategory.Safety };

        db.Skills.AddRange(combineDiag, dieselRepair, precisionAg, hydraulics, refrigerant, forkliftSkill);

        db.CertificationSkills.AddRange(
            new CertificationSkill { Certification = jdAgt2, Skill = combineDiag, GrantedProficiency = ProficiencyLevel.Intermediate },
            new CertificationSkill { Certification = jdAgt2, Skill = precisionAg, GrantedProficiency = ProficiencyLevel.Intermediate },
            new CertificationSkill { Certification = jdPrecision, Skill = precisionAg, GrantedProficiency = ProficiencyLevel.Advanced },
            new CertificationSkill { Certification = kubotaSvc, Skill = hydraulics, GrantedProficiency = ProficiencyLevel.Intermediate },
            new CertificationSkill { Certification = epa608, Skill = refrigerant, GrantedProficiency = ProficiencyLevel.Intermediate });

        db.TrainingProgramSkills.AddRange(
            new TrainingProgramSkill { TrainingProgram = forklift, Skill = forkliftSkill, GrantedProficiency = ProficiencyLevel.Beginner },
            new TrainingProgramSkill { TrainingProgram = jdOnboarding, Skill = combineDiag, GrantedProficiency = ProficiencyLevel.Beginner });

        db.RoleRequirements.AddRange(
            new RoleRequirement { JobRole = agTechRole, Kind = RequirementKind.Certification, Certification = jdAgt1, IsMandatory = true, DueWithinDaysOfHire = 90 },
            new RoleRequirement { JobRole = agTechRole, Kind = RequirementKind.Certification, Certification = jdAgt2, IsMandatory = true, DueWithinDaysOfHire = 180 },
            new RoleRequirement { JobRole = agTechRole, Kind = RequirementKind.Certification, Certification = osha10, IsMandatory = true, DueWithinDaysOfHire = 30 },
            new RoleRequirement { JobRole = agTechRole, Kind = RequirementKind.Training, TrainingProgram = oshaRefresher, IsMandatory = true, DueWithinDaysOfHire = 45 },
            new RoleRequirement { JobRole = agTechRole, Kind = RequirementKind.Training, TrainingProgram = forklift, IsMandatory = true, DueWithinDaysOfHire = 14 },
            new RoleRequirement { JobRole = dieselTechRole, Kind = RequirementKind.Certification, Certification = epa608, IsMandatory = true, DueWithinDaysOfHire = 60 },
            new RoleRequirement { JobRole = dieselTechRole, Kind = RequirementKind.Training, TrainingProgram = epaSpill, IsMandatory = true, DueWithinDaysOfHire = 30 },
            new RoleRequirement { JobRole = serviceManagerRole, Kind = RequirementKind.Training, TrainingProgram = oshaRefresher, IsMandatory = true });

        var marcus = new Employee
        {
            FirstName = "Marcus",
            LastName = "Chen",
            Email = "marcus.chen@caliber-demo.com",
            ExternalEmployeeNo = "E1001",
            JobRole = serviceManagerRole,
            Location = springfield,
            HireDate = today.AddYears(-8),
            AccessLevel = AccessLevel.Admin,
        };

        var sarah = new Employee
        {
            FirstName = "Sarah",
            LastName = "Mitchell",
            Email = "sarah.mitchell@caliber-demo.com",
            ExternalEmployeeNo = "E1002",
            JobRole = serviceManagerRole,
            Location = springfield,
            HireDate = today.AddYears(-5),
            AccessLevel = AccessLevel.Manager,
        };

        var jake = new Employee
        {
            FirstName = "Jake",
            LastName = "Morrison",
            Email = "jake.morrison@caliber-demo.com",
            ExternalEmployeeNo = "E1003",
            JobRole = agTechRole,
            Location = springfield,
            HireDate = today.AddYears(-3),
            AccessLevel = AccessLevel.Technician,
        };

        var elena = new Employee
        {
            FirstName = "Elena",
            LastName = "Rodriguez",
            Email = "elena.rodriguez@caliber-demo.com",
            ExternalEmployeeNo = "E1004",
            JobRole = dieselTechRole,
            Location = desMoines,
            HireDate = today.AddYears(-4),
            AccessLevel = AccessLevel.Technician,
        };

        var tommy = new Employee
        {
            FirstName = "Tommy",
            LastName = "Walsh",
            Email = "tommy.walsh@caliber-demo.com",
            ExternalEmployeeNo = "E1005",
            JobRole = agTechRole,
            Location = desMoines,
            HireDate = today.AddYears(-6),
            AccessLevel = AccessLevel.Technician,
        };

        var priya = new Employee
        {
            FirstName = "Priya",
            LastName = "Patel",
            Email = "priya.patel@caliber-demo.com",
            ExternalEmployeeNo = "E1006",
            JobRole = partsRole,
            Location = springfield,
            HireDate = today.AddYears(-2),
            AccessLevel = AccessLevel.Technician,
        };

        var chris = new Employee
        {
            FirstName = "Chris",
            LastName = "Nguyen",
            Email = "chris.nguyen@caliber-demo.com",
            ExternalEmployeeNo = "E1007",
            JobRole = salesRole,
            Location = omaha,
            HireDate = today.AddYears(-1),
            AccessLevel = AccessLevel.Technician,
        };

        var jordan = new Employee
        {
            FirstName = "Jordan",
            LastName = "Lee",
            Email = "jordan.lee@caliber-demo.com",
            ExternalEmployeeNo = "E1008",
            JobRole = serviceManagerRole,
            Location = omaha,
            HireDate = today.AddYears(-7),
            AccessLevel = AccessLevel.Manager,
        };

        var avery = new Employee
        {
            FirstName = "Avery",
            LastName = "Brooks",
            Email = "avery.brooks@caliber-demo.com",
            ExternalEmployeeNo = "E1009",
            JobRole = agTechRole,
            Location = springfield,
            HireDate = today.AddDays(-18),
            AccessLevel = AccessLevel.Technician,
        };

        var dana = new Employee
        {
            FirstName = "Dana",
            LastName = "Kim",
            Email = "dana.kim@caliber-demo.com",
            ExternalEmployeeNo = "E1010",
            JobRole = agTechRole,
            Location = omaha,
            HireDate = today.AddYears(-2),
            AccessLevel = AccessLevel.Technician,
        };

        var mike = new Employee
        {
            FirstName = "Mike",
            LastName = "O'Brien",
            Email = "mike.obrien@caliber-demo.com",
            ExternalEmployeeNo = "E1011",
            JobRole = dieselTechRole,
            Location = springfield,
            HireDate = today.AddYears(-3),
            AccessLevel = AccessLevel.Technician,
        };

        var lisa = new Employee
        {
            FirstName = "Lisa",
            LastName = "Hart",
            Email = "lisa.hart@caliber-demo.com",
            ExternalEmployeeNo = "E1012",
            JobRole = partsRole,
            Location = desMoines,
            HireDate = today.AddYears(-2),
            AccessLevel = AccessLevel.Technician,
        };

        db.Employees.AddRange(marcus, sarah, jake, elena, tommy, priya, chris, jordan, avery, dana, mike, lisa);

        // Jake — compliant baseline technician persona (JD-AGT1 + OSHA-10 current; JD-PRE expiring soon).
        AddCertAssignment(jake, jdAgt1, AssignmentStatus.Completed, today.AddYears(-1), today.AddYears(-1), today.AddYears(2), recordedBy, now);
        AddCertAssignment(jake, jdPrecision, AssignmentStatus.Completed, today.AddMonths(-11), today.AddMonths(-11), today.AddMonths(1), recordedBy, now);
        AddCertAssignment(jake, osha10, AssignmentStatus.Completed, today.AddYears(-2), today.AddYears(-2), null, recordedBy, now);
        AddTrainingAssignment(jake, oshaRefresher, AssignmentStatus.Completed, today.AddMonths(-2), today.AddMonths(-2), today.AddMonths(10), 100, recordedBy, now);
        AddTrainingAssignment(jake, forklift, AssignmentStatus.Completed, today.AddYears(-1), today.AddYears(-1), null, 100, recordedBy, now);

        db.EmployeeSkills.AddRange(
            new EmployeeSkill
            {
                Employee = jake,
                Skill = precisionAg,
                ProficiencyLevel = ProficiencyLevel.Intermediate,
                SourceType = SkillSourceType.Certification,
                SourceCertification = jdPrecision,
                AssessedOn = today.AddMonths(-11),
                AssessedBy = recordedBy,
            },
            new EmployeeSkill
            {
                Employee = jake,
                Skill = combineDiag,
                ProficiencyLevel = ProficiencyLevel.Intermediate,
                SourceType = SkillSourceType.Certification,
                SourceCertification = jdAgt2,
                AssessedOn = today.AddMonths(-6),
                AssessedBy = recordedBy,
                Status = EmployeeSkillStatus.Active,
            });

        // Sarah — location manager with mostly compliant profile.
        AddCertAssignment(sarah, osha10, AssignmentStatus.Completed, today.AddYears(-3), today.AddYears(-3), null, recordedBy, now);
        AddTrainingAssignment(sarah, oshaRefresher, AssignmentStatus.Completed, today.AddMonths(-1), today.AddMonths(-1), today.AddMonths(11), 100, recordedBy, now);

        // Elena — expiring soon (Kubota cert expires within warning window).
        AddCertAssignment(elena, kubotaSvc, AssignmentStatus.Completed, today.AddMonths(-23), today.AddMonths(-23), today.AddDays(20), recordedBy, now);
        AddCertAssignment(elena, epa608, AssignmentStatus.Completed, today.AddYears(-2), today.AddYears(-2), today.AddYears(3), recordedBy, now);
        AddTrainingAssignment(elena, epaSpill, AssignmentStatus.Completed, today.AddMonths(-6), today.AddMonths(-6), today.AddMonths(18), 100, recordedBy, now);

        // Tommy — expired John Deere Level 2.
        AddCertAssignment(tommy, jdAgt2, AssignmentStatus.Completed, today.AddYears(-3), today.AddYears(-3), today.AddMonths(-6), recordedBy, now);
        AddCertAssignment(tommy, jdAgt1, AssignmentStatus.Completed, today.AddYears(-4), today.AddYears(-4), today.AddYears(-1), recordedBy, now);

        // Dana — waived OSHA requirement.
        AddCertAssignment(dana, jdAgt1, AssignmentStatus.Completed, today.AddYears(-1), today.AddYears(-1), today.AddYears(2), recordedBy, now);
        db.EmployeeCertifications.Add(new EmployeeCertification
        {
            Employee = dana,
            Certification = osha10,
            Status = AssignmentStatus.Waived,
            Source = AssignmentSource.RoleTemplate,
            AssignedOn = today.AddYears(-2),
            DueOn = today.AddYears(-2).AddDays(30),
            Notes = "Grandfathered from prior employer documentation.",
        });

        // Mike — overdue training (due date passed, not started).
        AddCertAssignment(mike, epa608, AssignmentStatus.Completed, today.AddYears(-1), today.AddYears(-1), today.AddYears(4), recordedBy, now);
        db.EmployeeTrainings.Add(new EmployeeTraining
        {
            Employee = mike,
            TrainingProgram = epaSpill,
            Status = AssignmentStatus.NotStarted,
            Source = AssignmentSource.RoleTemplate,
            AssignedOn = today.AddMonths(-4),
            DueOn = today.AddMonths(-1),
            PercentComplete = 0,
        });

        // Lisa — in-progress onboarding training.
        db.EmployeeTrainings.Add(new EmployeeTraining
        {
            Employee = lisa,
            TrainingProgram = jdOnboarding,
            Status = AssignmentStatus.InProgress,
            Source = AssignmentSource.Direct,
            AssignedOn = today.AddDays(-10),
            DueOn = today.AddDays(20),
            StartedOn = today.AddDays(-7),
            PercentComplete = 45,
        });

        // Avery — new hire with role-template checklist, all not started (empty readiness).
        foreach (var requirement in new[] { jdAgt1, jdAgt2, osha10 })
        {
            db.EmployeeCertifications.Add(new EmployeeCertification
            {
                Employee = avery,
                Certification = requirement,
                Status = AssignmentStatus.NotStarted,
                Source = AssignmentSource.RoleTemplate,
                AssignedOn = avery.HireDate,
                DueOn = avery.HireDate.AddDays(requirement == osha10 ? 30 : requirement == jdAgt1 ? 90 : 180),
            });
        }

        foreach (var program in new[] { oshaRefresher, forklift })
        {
            db.EmployeeTrainings.Add(new EmployeeTraining
            {
                Employee = avery,
                TrainingProgram = program,
                Status = AssignmentStatus.NotStarted,
                Source = AssignmentSource.RoleTemplate,
                AssignedOn = avery.HireDate,
                DueOn = avery.HireDate.AddDays(program == forklift ? 14 : 45),
                PercentComplete = 0,
            });
        }

        // Priya — compliant parts employee with product training complete.
        AddTrainingAssignment(priya, kubotaProduct, AssignmentStatus.Completed, today.AddMonths(-3), today.AddMonths(-3), null, 100, recordedBy, now);

        // Chris — sales with missing mandatory safety cert.
        db.EmployeeCertifications.Add(new EmployeeCertification
        {
            Employee = chris,
            Certification = osha10,
            Status = AssignmentStatus.NotStarted,
            Source = AssignmentSource.Direct,
            AssignedOn = today.AddMonths(-2),
            DueOn = today.AddDays(15),
        });

        // Jordan — Omaha manager, compliant refresher.
        AddTrainingAssignment(jordan, oshaRefresher, AssignmentStatus.Completed, today.AddMonths(-3), today.AddMonths(-3), today.AddMonths(9), 100, recordedBy, now);

        // Marcus — admin overview, mixed statuses across credentials.
        AddCertAssignment(marcus, jdAgt1, AssignmentStatus.Completed, today.AddYears(-5), today.AddYears(-5), today.AddYears(-2), recordedBy, now);
        AddTrainingAssignment(marcus, oshaRefresher, AssignmentStatus.InProgress, today.AddDays(-5), null, null, 30, recordedBy, now,
            dueOn: today.AddDays(25), startedOn: today.AddDays(-5));

        await db.SaveChangesAsync(cancellationToken);
    }

    private static void AddCertAssignment(
        Employee employee,
        Certification certification,
        AssignmentStatus status,
        DateOnly assignedOn,
        DateOnly awardedOn,
        DateOnly? expiresOn,
        string recordedBy,
        DateTimeOffset recordedAt)
    {
        var assignment = new EmployeeCertification
        {
            Employee = employee,
            Certification = certification,
            Status = status,
            Source = AssignmentSource.RoleTemplate,
            AssignedOn = assignedOn,
            DueOn = assignedOn.AddDays(90),
        };

        if (status == AssignmentStatus.Completed)
        {
            assignment.Awards.Add(new CertificationAward
            {
                AwardedOn = awardedOn,
                ExpiresOn = expiresOn,
                CertificateNumber = $"{certification.Code}-{employee.ExternalEmployeeNo}",
                RecordedBy = recordedBy,
                RecordedAt = recordedAt,
            });
        }

        assignment.Employee.Certifications.Add(assignment);
    }

    private static void AddTrainingAssignment(
        Employee employee,
        TrainingProgram program,
        AssignmentStatus status,
        DateOnly assignedOn,
        DateOnly? completedOn,
        DateOnly? nextDueOn,
        int percentComplete,
        string recordedBy,
        DateTimeOffset recordedAt,
        DateOnly? dueOn = null,
        DateOnly? startedOn = null)
    {
        var assignment = new EmployeeTraining
        {
            Employee = employee,
            TrainingProgram = program,
            Status = status,
            Source = AssignmentSource.RoleTemplate,
            AssignedOn = assignedOn,
            DueOn = dueOn ?? assignedOn.AddDays(45),
            StartedOn = startedOn,
            CompletedOn = completedOn,
            NextDueOn = nextDueOn,
            PercentComplete = percentComplete,
        };

        if (status == AssignmentStatus.Completed && program.RequiresAcknowledgement)
        {
            assignment.AcknowledgedOn = recordedAt;
            assignment.AcknowledgedBy = recordedBy;
        }

        employee.Trainings.Add(assignment);
    }
}
