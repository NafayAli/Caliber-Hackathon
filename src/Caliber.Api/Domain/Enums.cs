namespace Caliber.Api.Domain;

public enum CertificationCategory
{
    Oem = 1,
    Safety = 2,
    Regulatory = 3,
    Internal = 4,
}

public enum TrainingCategory
{
    Oem = 1,
    Safety = 2,
    Onboarding = 3,
    Product = 4,
    Internal = 5,
}

public enum DeliveryMode
{
    Online = 1,
    InPerson = 2,
    OnTheJob = 3,
    Document = 4,
}

public enum SkillCategory
{
    Oem = 1,
    EquipmentType = 2,
    SystemType = 3,
    Safety = 4,
}

public enum ProficiencyLevel
{
    Beginner = 1,
    Intermediate = 2,
    Advanced = 3,
    Expert = 4,
}

/// <summary>Stored state of an assignment. Distinct from the computed <see cref="ReadinessStatus"/>.</summary>
public enum AssignmentStatus
{
    NotStarted = 1,
    InProgress = 2,
    Completed = 3,
    Waived = 4,
}

public enum AssignmentSource
{
    RoleTemplate = 1,
    Direct = 2,
}

public enum RequirementKind
{
    Certification = 1,
    Training = 2,
    Skill = 3,
}

public enum SkillSourceType
{
    Certification = 1,
    Training = 2,
    Experience = 3,
    ManagerAssessed = 4,
}

public enum EmployeeSkillStatus
{
    Active = 1,
    Expired = 2,
    PendingApproval = 3,
    Rejected = 4,
}

public enum SkillRequestStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
}

public enum EvidenceType
{
    Certificate = 1,
    Acknowledgement = 2,
    Scan = 3,
    Photo = 4,
    Other = 5,

    /// <summary>General employee evidence not tied to a specific assignment.</summary>
    General = 6,
}

/// <summary>What a person may see and do. Drives resource-level query scoping.</summary>
public enum AccessLevel
{
    /// <summary>Sees only their own record.</summary>
    Technician = 1,

    /// <summary>Sees every employee at their own location.</summary>
    Manager = 2,

    /// <summary>Sees every employee at every location.</summary>
    Admin = 3,
}

/// <summary>
/// Derived from dates and stored status at read time; never persisted, so no two
/// screens can disagree about whether someone is compliant.
/// </summary>
public enum ReadinessStatus
{
    Compliant = 1,
    ExpiringSoon = 2,
    Expired = 3,
    Overdue = 4,
    InProgress = 5,
    Missing = 6,
    Waived = 7,
}

public enum NotificationKind
{
    Announcement = 1,
    ExpiryAlert = 2,
    PendingRequirement = 3,
    Acknowledgement = 4,
    RenewalRequest = 5,
    RenewalDecision = 6,
    Reminder = 7,
    System = 8,
}

public enum RenewalRequestStatus
{
    Pending = 1,
    Approved = 2,
    Declined = 3,
}
