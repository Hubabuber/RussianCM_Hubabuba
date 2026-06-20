using System.Linq;
using Content.Server.Administration;
using Content.Server.Chat.Systems;
using Content.Server.Humanoid;
using Content.Shared._CMU14.Acquaintance;
using Content.Shared._RMC14.Dialog;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.NPC.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Utility;

using Robust.Shared.Enums;

namespace Content.Server._CMU14.Acquaintance;

/// <summary>
/// Server-authoritative recognition of faces and voices.
/// </summary>
public sealed partial class AcquaintanceSystem : EntitySystem
{
    private static readonly string[] AutomaticallyAcquaintedFactions =
    {
        "GOVFOR",
        "OPFOR",
        "AUWeYu",
    };

    private static readonly string[] ChatNameColors =
    {
        "#ef9a9a",
        "#90caf9",
        "#a5d6a7",
        "#ffe082",
        "#ce93d8",
        "#80deea",
        "#ffab91",
        "#bcaaa4",
        "#b0bec5",
        "#f48fb1",
        "#9fa8da",
        "#e6ee9c",
    };

    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private QuickDialogSystem _quickDialog = default!;
    [Dependency] private DialogSystem _dialog = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private IConfigurationManager _configuration = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetVerbsEvent<ExamineVerb>>(OnGetExamineVerbs);
        SubscribeLocalEvent<HumanoidAppearanceComponent, RememberIntroductionConfirmEvent>(OnRememberIntroductionConfirmed);
    }

    private void OnGetExamineVerbs(GetVerbsEvent<ExamineVerb> args)
    {
        if (!CanIntroduceCharacters(args.User, args.Target))
            return;

        var canIntroduce = args.CanAccess && args.CanInteract;
        args.Verbs.Add(new ExamineVerb
        {
            Text = Loc.GetString("acquaintance-introduce-verb"),
            Message = Loc.GetString(canIntroduce
                ? "acquaintance-introduce-verb-description"
                : "acquaintance-introduce-too-far"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/bubbles.svg.192dpi.png")),
            Priority = 20,
            Disabled = !canIntroduce,
            CloseMenu = true,
            Act = () => OpenIntroductionDialog(args.User, args.Target)
        });
    }

    private void OpenIntroductionDialog(EntityUid speaker, EntityUid listener)
    {
        if (!TryComp(speaker, out ActorComponent? actor) ||
            !CanIntroduceNow(speaker, listener))
        {
            return;
        }

        _quickDialog.OpenDialog(
            actor.PlayerSession,
            Loc.GetString("acquaintance-introduce-dialog-title"),
            Loc.GetString("acquaintance-introduce-dialog-prompt"),
            (string claimedName) => Introduce(speaker, listener, claimedName),
            defaultValue: Name(speaker));
    }

    private bool CanIntroduceCharacters(EntityUid speaker, EntityUid listener)
    {
        return speaker != listener &&
               HasComp<HumanoidAppearanceComponent>(speaker) &&
               HasComp<HumanoidAppearanceComponent>(listener) &&
               _mind.TryGetMind(speaker, out _, out _) &&
               _mind.TryGetMind(listener, out _, out _);
    }

    private bool CanIntroduceNow(EntityUid speaker, EntityUid listener)
    {
        return Exists(speaker) &&
               Exists(listener) &&
               CanIntroduceCharacters(speaker, listener) &&
               _interaction.InRangeUnobstructed(speaker, listener);
    }

    public void Introduce(EntityUid speaker, EntityUid listener, string? claimedName = null)
    {
        if (!CanIntroduceNow(speaker, listener) ||
            !_mind.TryGetMind(listener, out var listenerMindId, out _))
        {
            return;
        }

        claimedName = SanitizeClaimedName(claimedName) ??
                      Name(speaker);
        var unknownFace = GetUnknownFaceDescription(speaker);
        var voiceName = GetTransformedVoiceName(speaker);
        var faceVisible = CanSeeFace(speaker);

        _popup.PopupEntity(
            Loc.GetString("acquaintance-introduce-speaker", ("target", GetPerceivedFaceName(speaker, listener)), ("name", claimedName)),
            speaker,
            speaker,
            PopupType.Medium);

        _popup.PopupEntity(
            Loc.GetString("acquaintance-introduce-listener-pending",
                ("speaker", unknownFace),
                ("name", claimedName)),
            speaker,
            listener,
            PopupType.Medium);

        if (!HasComp<ActorComponent>(listener))
        {
            RememberIntroduction(speaker, listener, listenerMindId, claimedName, voiceName, faceVisible);
            return;
        }

        _dialog.OpenConfirmation(
            listener,
            Loc.GetString("acquaintance-remember-dialog-title"),
            Loc.GetString("acquaintance-remember-dialog-prompt",
                ("speaker", unknownFace),
                ("name", claimedName)),
            new RememberIntroductionConfirmEvent(
                GetNetEntity(speaker),
                claimedName,
                voiceName,
                faceVisible));
    }

    private void OnRememberIntroductionConfirmed(
        Entity<HumanoidAppearanceComponent> listener,
        ref RememberIntroductionConfirmEvent args)
    {
        if (!TryGetEntity(args.Speaker, out var speaker) ||
            !_mind.TryGetMind(listener, out var listenerMindId, out _))
        {
            return;
        }

        RememberIntroduction(
            speaker.Value,
            listener,
            listenerMindId,
            args.ClaimedName,
            args.VoiceName,
            args.FaceVisible);
    }

    private void RememberIntroduction(
        EntityUid speaker,
        EntityUid listener,
        EntityUid listenerMindId,
        string claimedName,
        string voiceName,
        bool faceVisible)
    {
        if (!Exists(speaker) || !Exists(listenerMindId))
            return;

        var memory = EnsureComp<AcquaintanceComponent>(listenerMindId);

        if (faceVisible)
            memory.KnownFaces[speaker] = claimedName;

        memory.KnownVoices[GetVoiceSignature(speaker, voiceName)] = claimedName;

        if (Exists(listener) && TryComp(listener, out ActorComponent? listenerActor))
        {
            RaiseNetworkEvent(
                new ExamineSystemMessages.PerceivedNamesResponseMessage(
                    new List<NetEntity> { GetNetEntity(speaker) },
                    new List<string> { GetPerceivedFaceName(listener, speaker) }),
                listenerActor.PlayerSession.Channel);
        }

        if (Exists(listener))
        {
            _popup.PopupEntity(
                Loc.GetString(
                    faceVisible ? "acquaintance-remember-accepted-face" : "acquaintance-remember-accepted-voice",
                    ("name", claimedName)),
                listener,
                listener,
                PopupType.Medium);
        }
    }

    private string? SanitizeClaimedName(string? claimedName)
    {
        if (string.IsNullOrWhiteSpace(claimedName))
            return null;

        var sanitized = new string(claimedName
            .Where(character => !char.IsControl(character))
            .ToArray())
            .Trim();

        if (sanitized.Length == 0)
            return null;

        var maxLength = _configuration.GetCVar(CCVars.MaxNameLength);
        return sanitized.Length > maxLength
            ? sanitized[..maxLength]
            : sanitized;
    }

    public string GetPerceivedFaceName(EntityUid viewer, EntityUid target)
    {
        if (viewer == target || HasComp<GhostComponent>(viewer))
            return Identity.Name(target, EntityManager, viewer).Name;

        if (!HasComp<HumanoidAppearanceComponent>(target))
            return Identity.Name(target, EntityManager, viewer).Name;

        if (!CanSeeFace(target))
            return GetUnknownFaceDescription(target);

        if (AreAutomaticallyAcquainted(viewer, target))
            return Name(target);

        if (TryGetMemory(viewer, out var memory) &&
            memory.KnownFaces.TryGetValue(target, out var knownName))
        {
            return knownName;
        }

        return GetUnknownFaceDescription(target);
    }

    public string GetPerceivedVoiceName(EntityUid viewer, EntityUid speaker, string transformedVoiceName)
    {
        if (viewer == speaker || HasComp<GhostComponent>(viewer))
            return transformedVoiceName;

        if (string.Equals(transformedVoiceName, Name(speaker), StringComparison.Ordinal) &&
            AreAutomaticallyAcquainted(viewer, speaker))
        {
            return Name(speaker);
        }

        if (TryGetMemory(viewer, out var memory) &&
            memory.KnownVoices.TryGetValue(GetVoiceSignature(speaker, transformedVoiceName), out var knownName))
        {
            return knownName;
        }

        return GetUnknownVoiceDescription(speaker, transformedVoiceName);
    }

    private bool AreAutomaticallyAcquainted(EntityUid first, EntityUid second)
    {
        return IsAutomaticallyAcquaintedFaction(first) &&
               IsAutomaticallyAcquaintedFaction(second);
    }

    private bool IsAutomaticallyAcquaintedFaction(EntityUid entity)
    {
        if (!TryComp(entity, out NpcFactionMemberComponent? factions))
            return false;

        foreach (var faction in AutomaticallyAcquaintedFactions)
        {
            if (factions.Factions.Contains(faction))
                return true;
        }

        return false;
    }

    public string GetColoredChatName(EntityUid source, string perceivedName)
    {
        var hash = unchecked((uint) source.Id * 2654435761u);
        var color = ChatNameColors[hash % ChatNameColors.Length];
        return $"[color={color}]{FormattedMessage.EscapeText(perceivedName)}[/color]";
    }

    public string GetUnknownFaceDescription(EntityUid target)
    {
        if (!TryComp(target, out HumanoidAppearanceComponent? appearance))
            return Loc.GetString("acquaintance-unknown-person");

        var age = _humanoid.GetAgeRepresentation(appearance.Species, appearance.Age);
        var gender = appearance.Gender switch
        {
            Gender.Female => Loc.GetString("identity-gender-feminine"),
            Gender.Male => Loc.GetString("identity-gender-masculine"),
            _ => Loc.GetString("identity-gender-person")
        };

        return Loc.GetString("acquaintance-unknown-face", ("gender", gender), ("age", age));
    }

    private string GetUnknownVoiceDescription(EntityUid speaker, string transformedVoiceName)
    {
        // A renamed voice is treated as electronically or otherwise altered, so its
        // owner's physical characteristics are not leaked through the description.
        if (!string.Equals(transformedVoiceName, Name(speaker), StringComparison.Ordinal) ||
            !TryComp(speaker, out HumanoidAppearanceComponent? appearance))
        {
            return Loc.GetString("acquaintance-unknown-voice");
        }

        return appearance.Gender switch
        {
            Gender.Female => Loc.GetString("acquaintance-unknown-voice-feminine"),
            Gender.Male => Loc.GetString("acquaintance-unknown-voice-masculine"),
            _ => Loc.GetString("acquaintance-unknown-voice")
        };
    }

    private bool TryGetMemory(EntityUid character, out AcquaintanceComponent memory)
    {
        if (_mind.TryGetMind(character, out var mindId, out _) &&
            TryComp(mindId, out AcquaintanceComponent? component))
        {
            memory = component;
            return true;
        }

        memory = default!;
        return false;
    }

    private bool CanSeeFace(EntityUid target)
    {
        var ev = new SeeIdentityAttemptEvent();
        RaiseLocalEvent(target, ev);
        return !ev.Cancelled;
    }

    private string GetTransformedVoiceName(EntityUid speaker)
    {
        var ev = new TransformSpeakerNameEvent(speaker, Name(speaker));
        RaiseLocalEvent(speaker, ev);
        return ev.VoiceName;
    }

    private static string GetVoiceSignature(EntityUid speaker, string transformedVoiceName)
    {
        return $"{speaker}:{transformedVoiceName.Trim().ToUpperInvariant()}";
    }
}
