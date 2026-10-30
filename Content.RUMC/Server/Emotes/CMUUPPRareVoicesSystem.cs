using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.CMU14.Traits.Quirks;
using Content.Shared.CMU14.Yautja;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Roles;
using Content.Shared.Speech.Components;
using Content.Shared.Speech.EntitySystems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.CMU14.Emotes;

public sealed partial class CMUUPPRareVoicesSystem : EntitySystem
{
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private IRobustRandom _random = default!;

    private static readonly ProtoId<EmoteSoundsPrototype> RareSounds = "CMUUPPRareEmotes";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawn);
        SubscribeLocalEvent<CMUUPPRareVoicesComponent, EmoteEvent>(OnEmote,
            before: [typeof(VocalSystem)], after: [typeof(CMUMuteSystem)]);
    }

    private void OnPlayerSpawn(PlayerSpawnCompleteEvent args)
    {
        // Read the spawned profile rather than allegiance or the currently selected lobby character.
        // This also works when character descriptions are disabled.
        if (args.Profile.Origin is not { } origin
            || !origin.Id.StartsWith("UPP", StringComparison.Ordinal)
            || !HasComp<HumanoidProfileComponent>(args.Mob)
            || !HasComp<VocalComponent>(args.Mob)
            || HasComp<YautjaComponent>(args.Mob))
        {
            return;
        }

        if (args.JobId != null && ProtoMan.TryIndex<JobPrototype>(args.JobId, out var job)
            && !job.UsePlayerProfile)
        {
            return;
        }

        EnsureComp<CMUUPPRareVoicesComponent>(args.Mob);
    }

    private void OnEmote(Entity<CMUUPPRareVoicesComponent> ent, ref EmoteEvent args)
    {
        if (args.Handled
            || args.Emote.ID is not ("Warcry" or "Corpsman")
            || !args.Emote.Category.HasFlag(EmoteCategory.Vocal)
            || !HasComp<VocalComponent>(ent)
            || !_random.Prob(ent.Comp.Chance))
        {
            return;
        }

        // Reuse the normal emote path so listener preferences and spatial audio still apply.
        // Handling the event suppresses the ordinary voice only when a rare sound was played.
        args.Handled = _chat.TryPlayEmoteSound(ent, ProtoMan.Index(RareSounds), args.Emote);
    }
}
