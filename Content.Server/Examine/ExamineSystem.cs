using System.Linq;
using Content.Server.Verbs;
using Content.Shared.Examine;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Utility;

namespace Content.Server.Examine
{
    [UsedImplicitly]
    public sealed class ExamineSystem : ExamineSystemShared
    {
        [Dependency] private readonly VerbSystem _verbSystem = default!;

        private static readonly FormattedMessage _entityNotFoundMessage;

        private static readonly FormattedMessage _entityOutOfRangeMessage;

        static ExamineSystem()
        {
            _entityNotFoundMessage = new FormattedMessage();
            _entityNotFoundMessage.AddText(Loc.GetString("examine-system-entity-does-not-exist"));
            _entityOutOfRangeMessage = new FormattedMessage();
            _entityOutOfRangeMessage.AddText(Loc.GetString("examine-system-cant-see-entity"));
        }

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<ExamineSystemMessages.RequestExamineInfoMessage>(ExamineInfoRequest);
        }

        public override void SendExamineTooltip(EntityUid player, EntityUid target, FormattedMessage message, bool getVerbs, bool centerAtCursor)
        {
            if (!TryComp<ActorComponent>(player, out var actor))
                return;

            var session = actor.PlayerSession;

            SortedSet<Verb>? verbs = null;
            if (getVerbs)
                verbs = _verbSystem.GetLocalVerbs(target, player, typeof(ExamineVerb));

            var ev = new ExamineSystemMessages.ExamineInfoResponseMessage(
                target, 0, message, verbs?.ToList(), centerAtCursor
            );

            RaiseNetworkEvent(ev, session.ConnectedClient);
        }

        private void ExamineInfoRequest(ExamineSystemMessages.RequestExamineInfoMessage request, EntitySessionEventArgs eventArgs)
        {
            var player = (IPlayerSession) eventArgs.SenderSession;
            var session = eventArgs.SenderSession;
            var channel = player.ConnectedClient;

            if (session.AttachedEntity is not {Valid: true} playerEnt
                || !EntityManager.EntityExists(request.EntityUid))
            {
                RaiseNetworkEvent(new ExamineSystemMessages.ExamineInfoResponseMessage(
                    request.EntityUid, request.Id, _entityNotFoundMessage), channel);
                return;
            }

            if (!CanExamine(playerEnt, request.EntityUid))
            {
                RaiseNetworkEvent(new ExamineSystemMessages.ExamineInfoResponseMessage(
                    request.EntityUid, request.Id, _entityOutOfRangeMessage, knowTarget: false), channel);
                return;
            }

            SortedSet<Verb>? verbs = null;
            if (request.GetVerbs)
                verbs = _verbSystem.GetLocalVerbs(request.EntityUid, playerEnt, typeof(ExamineVerb));

            var text = GetExamineText(request.EntityUid, player.AttachedEntity);
            RaiseNetworkEvent(new ExamineSystemMessages.ExamineInfoResponseMessage(
                request.EntityUid, request.Id, text, verbs?.ToList()), channel);
        }
    }
}
