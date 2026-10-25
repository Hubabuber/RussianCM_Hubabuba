using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    public static readonly CVarDef<string> CMUDiscordRulesWebhook =
        CVarDef.Create("cmu.discord.rules_webhook", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<string> CMUDiscordGuidesWebhook =
        CVarDef.Create("cmu.discord.guides_webhook", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>Seconds between reconciliations. Zero disables automatic publication.</summary>
    public static readonly CVarDef<int> CMUDiscordDocumentationInterval =
        CVarDef.Create("cmu.discord.documentation_interval", 900, CVar.SERVERONLY);
}
