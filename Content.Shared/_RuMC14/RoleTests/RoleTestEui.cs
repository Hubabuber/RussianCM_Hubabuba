using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._RuMC14.RoleTests;

[Serializable, NetSerializable]
public sealed class RoleTestEuiState(
    List<RoleTestEntry> tests,
    ActiveRoleTest? active,
    string? message,
    TimeSpan retryCooldown) : EuiStateBase
{
    public readonly List<RoleTestEntry> Tests = tests;
    public readonly ActiveRoleTest? Active = active;
    public readonly string? Message = message;
    public readonly TimeSpan RetryCooldown = retryCooldown;
}

[Serializable, NetSerializable]
public sealed record RoleTestEntry(
    string Id,
    string Name,
    string? Description,
    RoleTestResponsibility Responsibility,
    int QuestionCount,
    bool RequiresLaw,
    bool Passed,
    bool CanStart,
    int AvailableQuestions);

[Serializable, NetSerializable]
public sealed record ActiveRoleTest(
    string TestId,
    string Name,
    List<RoleTestQuestionData> Questions);

[Serializable, NetSerializable]
public sealed record RoleTestQuestionData(
    string Id,
    string Text,
    List<string> Answers);

public static class RoleTestEuiMsg
{
    [Serializable, NetSerializable]
    public sealed class StartTest(string testId) : EuiMessageBase
    {
        public readonly string TestId = testId;
    }

    [Serializable, NetSerializable]
    public sealed class SubmitTest(string testId, Dictionary<string, int> answers) : EuiMessageBase
    {
        public readonly string TestId = testId;
        public readonly Dictionary<string, int> Answers = answers;
    }

    [Serializable, NetSerializable]
    public sealed class CancelTest : EuiMessageBase;
}
