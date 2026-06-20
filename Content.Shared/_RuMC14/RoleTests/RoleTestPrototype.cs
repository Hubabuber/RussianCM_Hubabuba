using Robust.Shared.Prototypes;
using Content.Shared.Roles;

namespace Content.Shared._RuMC14.RoleTests;

[Prototype("roleTest")]
public sealed partial class RoleTestPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Name = string.Empty;

    [DataField]
    public string? Description;

    [DataField(required: true)]
    public RoleTestResponsibility Responsibility;

    [DataField(required: true)]
    public int QuestionCount;

    [DataField]
    public HashSet<string> QuestionPools = new() { RoleTestShared.CommonPool };

    [DataField]
    public Dictionary<string, int> RequiredPools = new();

    [DataField]
    public bool RequiresLaw;
}

[Prototype("roleTestQuestion")]
public sealed partial class RoleTestQuestionPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Text = string.Empty;

    [DataField(required: true)]
    public List<string> Answers = new();

    [DataField(required: true)]
    public int CorrectAnswer;

    [DataField]
    public HashSet<string> Pools = new() { RoleTestShared.CommonPool };

    [DataField]
    public string? Source;
}

[Prototype("roleTestQuestionPool")]
public sealed partial class RoleTestQuestionPoolPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public ProtoId<JobPrototype> Job;

    [DataField(required: true)]
    public string Pool = string.Empty;
}

public enum RoleTestResponsibility : byte
{
    Low,
    Medium,
    High,
}

public static class RoleTestShared
{
    public const string CommonPool = "common";
    public const string LawPool = "law";
    public const string JobTestPrefix = "Job:";
    public const string JobQuestionPoolPrefix = "job:";

    public static string GetTracker(string testId)
    {
        return $"RoleTest:{testId}";
    }

    public static string GetJobTestId(string jobId)
    {
        return $"{JobTestPrefix}{jobId}";
    }

    public static string GetJobQuestionPool(string jobId)
    {
        return $"{JobQuestionPoolPrefix}{jobId}";
    }

    public static bool TryGetJobId(string testId, out string jobId)
    {
        if (testId.StartsWith(JobTestPrefix, StringComparison.Ordinal))
        {
            jobId = testId[JobTestPrefix.Length..];
            return true;
        }

        jobId = string.Empty;
        return false;
    }

    public static bool IsRoleTestExempt(JobPrototype job)
    {
        return job.RoleTestExempt ||
               job.ID == "AU14JobCivilianColonist" ||
               job.ID.Contains("Colonist", StringComparison.OrdinalIgnoreCase);
    }

    public static bool RequiresLaw(JobPrototype job)
    {
        return job.RoleTestRequiresLaw || IsMilitaryJob(job);
    }

    public static RoleTestResponsibility GetResponsibility(JobPrototype job)
    {
        if (job.RoleTestResponsibility != RoleTestResponsibility.Low)
            return job.RoleTestResponsibility;

        if (job.MarineAuthorityLevel >= 7)
            return RoleTestResponsibility.High;

        if (job.MarineAuthorityLevel >= 2)
            return RoleTestResponsibility.Medium;

        return RoleTestResponsibility.Low;
    }

    public static int GetQuestionCount(RoleTestResponsibility responsibility)
    {
        return responsibility switch
        {
            RoleTestResponsibility.Low => 10,
            RoleTestResponsibility.Medium => 25,
            RoleTestResponsibility.High => 50,
            _ => 10,
        };
    }

    public static int GetRequiredLawQuestionCount(RoleTestResponsibility responsibility)
    {
        return responsibility switch
        {
            RoleTestResponsibility.Low => 2,
            RoleTestResponsibility.Medium => 5,
            RoleTestResponsibility.High => 10,
            _ => 2,
        };
    }

    public static int GetRequiredCommonQuestionCount(RoleTestResponsibility responsibility)
    {
        return responsibility switch
        {
            RoleTestResponsibility.Low => 4,
            RoleTestResponsibility.Medium => 10,
            RoleTestResponsibility.High => 20,
            _ => 4,
        };
    }

    public static int GetRequiredRoleQuestionCount(RoleTestResponsibility responsibility, bool requiresLaw)
    {
        var questionCount = GetQuestionCount(responsibility);
        var commonCount = GetRequiredCommonQuestionCount(responsibility);
        var lawCount = requiresLaw ? GetRequiredLawQuestionCount(responsibility) : 0;

        return Math.Max(0, questionCount - commonCount - lawCount);
    }

    public static bool IsMilitaryJob(JobPrototype job)
    {
        if (job.MarineAuthorityLevel > 0)
            return true;

        return job.ID.Contains("Military", StringComparison.OrdinalIgnoreCase) ||
               job.ID.Contains("GOVFOR", StringComparison.OrdinalIgnoreCase) ||
               job.ID.Contains("OPFOR", StringComparison.OrdinalIgnoreCase) ||
               job.ID.Contains("RMCPlatoon", StringComparison.OrdinalIgnoreCase) ||
               job.ID.StartsWith("CM", StringComparison.OrdinalIgnoreCase) &&
               !job.ID.Contains("Survivor", StringComparison.OrdinalIgnoreCase);
    }
}
