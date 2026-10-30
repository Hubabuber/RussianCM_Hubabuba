using Robust.Shared.Configuration;
using Robust.Shared.Serialization;
using Content.Shared.Eui;

namespace Content.Shared._RuCM.Kaban;

// CMU14: account-owned reward economy; client requests never carry an authoritative actor.
[CVarDefs]
public sealed class KabanCVars
{
    public static readonly CVarDef<bool> Enabled = CVarDef.Create("rucm.kaban.enabled", false, CVar.SERVERONLY);
    public static readonly CVarDef<bool> Shop = CVarDef.Create("rucm.kaban.shop", false, CVar.SERVERONLY);
    public static readonly CVarDef<bool> Priorities = CVarDef.Create("rucm.kaban.priorities", false, CVar.SERVERONLY);
    public static readonly CVarDef<bool> Forcing = CVarDef.Create("rucm.kaban.forcing", false, CVar.SERVERONLY);
    public static readonly CVarDef<bool> Tts = CVarDef.Create("rucm.kaban.tts", false, CVar.SERVERONLY);
    public static readonly CVarDef<bool> Discord = CVarDef.Create("rucm.kaban.discord", false, CVar.SERVERONLY);
    public static readonly CVarDef<string> Managers = CVarDef.Create("rucm.kaban.managers", "", CVar.SERVERONLY);
    public static readonly CVarDef<string> SeniorManagers = CVarDef.Create("rucm.kaban.senior_managers", "", CVar.SERVERONLY);
    public static readonly CVarDef<string> ServerId = CVarDef.Create("rucm.kaban.server_id", "RussianCM", CVar.SERVERONLY);
    public static readonly CVarDef<string> DiscordGuild = CVarDef.Create("rucm.kaban.discord_guild", "", CVar.SERVERONLY);
    public static readonly CVarDef<string> DiscordBoundaryRole = CVarDef.Create("rucm.kaban.discord_boundary_role", "", CVar.SERVERONLY);
}

[Serializable, NetSerializable]
public enum KabanAction : byte
{
    Refresh, Purchase, Nominate, Approve, Reject, Arm, Disarm, CancelRequest,
    ApproveVoice, RejectPrivilege, SetProduct, CorrectBalance, ApproveDiscord, GrantVoice,
}

[Serializable, NetSerializable]
public sealed class KabanRequest : EuiMessageBase
{
    public Guid Operation { get; set; } = Guid.NewGuid();
    public KabanAction Action { get; set; }
    public Guid Target { get; set; }
    public string? TargetName { get; set; }
    public Guid Item { get; set; }
    public string Product { get; set; } = "";
    public string Text { get; set; } = "";
    public string Category { get; set; } = "";
    public string[] Jobs { get; set; } = [];
    public int Amount { get; set; }
    public bool Confirmed { get; set; }
    public bool Exceptional { get; set; }
}

[Serializable, NetSerializable]
public sealed record KabanProductView(string Id, int Price, bool Available, string Blocker);

[Serializable, NetSerializable]
public sealed record KabanItemView(Guid Id, string Kind, string Description, long Expires, string Status,
    int Multiplier = 0, bool Flexible = false, string[]? Jobs = null, string Detail = "", int Position = 0, string OwnerName = "");

[Serializable, NetSerializable]
public sealed record KabanHistoryView(Guid Id, long At, string Description, long Amount);

[Serializable, NetSerializable]
public sealed record KabanAwardView(Guid Id, Guid Recipient, Guid Initiator, int Amount,
    string Category, string Reason, string Status, Guid[] Approvals, long At);

[Serializable, NetSerializable]
public sealed record KabanWalletView(Guid UserId, long Balance, long Earned);

[Serializable, NetSerializable]
public sealed class KabanState : EuiStateBase
{
    public long Balance;
    public long Earned;
    public Guid Viewer;
    public bool Manager;
    public bool Senior;
    public bool PrioritiesEnabled;
    public bool Lobby;
    public string Result = "";
    public Guid Response;
    public List<KabanProductView> Products = [];
    public List<KabanItemView> Items = [];
    public List<KabanHistoryView> History = [];
    public List<KabanAwardView> Awards = [];
    public List<KabanHistoryView> Audit = [];
    public Dictionary<string, string> Jobs = new();
    public Dictionary<Guid, string> Accounts = new();
    public List<KabanWalletView> Wallets = [];
    public List<KabanForceOption> ForceOptions = [];
    public bool ForcingEnabled;
}

[Serializable, NetSerializable]
public sealed class KabanOpenEvent : EntityEventArgs;
