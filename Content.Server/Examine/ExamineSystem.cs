using System.Linq;
using Content.Server.Verbs;
using Content.Server._CMU14.Acquaintance;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Examine
{
    [UsedImplicitly]
    public sealed partial class ExamineSystem : ExamineSystemShared
    {
        [Dependency] private VerbSystem _verbSystem = default!;
        [Dependency] private AcquaintanceSystem _acquaintance = default!;

        private readonly FormattedMessage _entityNotFoundMessage = new();
        private readonly FormattedMessage _entityOutOfRangeMessage = new();

        public override void Initialize()
        {
            base.Initialize();
            _entityNotFoundMessage.AddText(Loc.GetString("examine-system-entity-does-not-exist"));
            _entityOutOfRangeMessage.AddText(Loc.GetString("examine-system-cant-see-entity"));

            SubscribeNetworkEvent<ExamineSystemMessages.RequestExamineInfoMessage>(ExamineInfoRequest);
            SubscribeNetworkEvent<ExamineSystemMessages.RequestPerceivedNamesMessage>(PerceivedNamesRequest);
        }

        public override void SendExamineTooltip(EntityUid player, EntityUid target, FormattedMessage message, bool getVerbs, bool centerAtCursor)
        {
            if (!TryComp<ActorComponent>(player, out var actor))
                return;

            var session = actor.PlayerSession;

            SortedSet<Verb>? verbs = null;
            if (getVerbs)
                verbs = _verbSystem.GetLocalVerbs(target, player, typeof(ExamineVerb));

            var displayName = HasComp<HumanoidAppearanceComponent>(target)
                ? _acquaintance.GetPerceivedFaceName(player, target)
                : null;
            var ev = new ExamineSystemMessages.ExamineInfoResponseMessage(
                GetNetEntity(target), 0, message, verbs?.ToList(), centerAtCursor, displayName: displayName
            );

            RaiseNetworkEvent(ev, session.Channel);
        }

        private void ExamineInfoRequest(ExamineSystemMessages.RequestExamineInfoMessage request, EntitySessionEventArgs eventArgs)
        {
            var player = eventArgs.SenderSession;
            var session = eventArgs.SenderSession;
            var channel = player.Channel;
            var entity = GetEntity(request.NetEntity);

            if (session.AttachedEntity is not {Valid: true} playerEnt
                || !Exists(entity))
            {
                RaiseNetworkEvent(new ExamineSystemMessages.ExamineInfoResponseMessage(
                    request.NetEntity, request.Id, _entityNotFoundMessage), channel);
                return;
            }

            if (!CanExamine(playerEnt, entity))
            {
                RaiseNetworkEvent(new ExamineSystemMessages.ExamineInfoResponseMessage(
                    request.NetEntity, request.Id, _entityOutOfRangeMessage, knowTarget: false), channel);
                return;
            }

            SortedSet<Verb>? verbs = null;
            if (request.GetVerbs)
                verbs = _verbSystem.GetLocalVerbs(entity, playerEnt, typeof(ExamineVerb));

            var text = GetExamineText(entity, player.AttachedEntity);
            var displayName = HasComp<HumanoidAppearanceComponent>(entity)
                ? _acquaintance.GetPerceivedFaceName(playerEnt, entity)
                : null;
            RaiseNetworkEvent(new ExamineSystemMessages.ExamineInfoResponseMessage(
                request.NetEntity, request.Id, text, verbs?.ToList(), displayName: displayName), channel);
        }

        private void PerceivedNamesRequest(
            ExamineSystemMessages.RequestPerceivedNamesMessage request,
            EntitySessionEventArgs eventArgs)
        {
            var session = eventArgs.SenderSession;
            if (session.AttachedEntity is not { Valid: true } player)
                return;

            var entities = new List<NetEntity>();
            var names = new List<string>();

            foreach (var netEntity in request.Entities.Distinct().Take(128))
            {
                if (!TryGetEntity(netEntity, out var entity) ||
                    !HasComp<HumanoidAppearanceComponent>(entity) ||
                    !CanExamine(player, entity.Value))
                {
                    continue;
                }

                entities.Add(netEntity);
                names.Add(_acquaintance.GetPerceivedFaceName(player, entity.Value));
            }

            RaiseNetworkEvent(
                new ExamineSystemMessages.PerceivedNamesResponseMessage(entities, names),
                session.Channel);
        }
    }
}
