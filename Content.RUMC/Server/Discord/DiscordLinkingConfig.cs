using Robust.Shared.Configuration;

namespace Content.Server._RuCM.Discord;

[CVarDefs]
public sealed class DiscordLinkingCVars
{
    public static readonly CVarDef<bool> Required = CVarDef.Create(
        "rmc.discord_account_linking_required", false, CVar.SERVERONLY);

    public static readonly CVarDef<string> Channel = CVarDef.Create(
        "rmc.discord_account_linking_channel", "", CVar.SERVERONLY);
}
