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
                target, message, verbs?.ToList(), centerAtCursor
            );

            RaiseNetworkEvent(ev, session.ConnectedClient);
        }

        public static FormattedMessage GetExamineGroupMessage(ExamineGroupEvent ev)
        {
            var formattedMessage = new FormattedMessage();

            ev.Entries.Sort((a, b) => (a.Priority.CompareTo(b.Priority)));

            formattedMessage.AddMarkup(ev.FirstLine);

            foreach (var entry in ev.Entries)
            {
                formattedMessage.PushNewline();
                formattedMessage.AddMarkup(entry.Markup);
            }

            return formattedMessage;
        }

        public override void AddExamineGroupVerb(string key, GetVerbsEvent<ExamineVerb> examineVerbsEvent, string iconTexture)
        {

            foreach (var verb in examineVerbsEvent.Verbs)
            {
                if (verb.Text == Loc.GetString("examine-system-" + key + "-text"))
                    return;
            }

            var examineVerb = new ExamineVerb()
            {
                Act = () =>
                {
                    var ev = new ExamineGroupEvent { FirstLine = Loc.GetString("examine-system-" + key + "-title") };
                    RaiseLocalEvent(examineVerbsEvent.Target, ev);
                    SendExamineTooltip(examineVerbsEvent.User, examineVerbsEvent.Target, GetExamineGroupMessage(ev), false, false);
                },
                Text = Loc.GetString("examine-system-" + key + "-text"),
                Message = Loc.GetString("examine-system-" + key + "-message"),
                Category = VerbCategory.Examine,
                IconTexture = iconTexture
            };

            examineVerbsEvent.Verbs.Add(examineVerb);
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
                    request.EntityUid, _entityNotFoundMessage), channel);
                return;
            }

            if (!CanExamine(playerEnt, request.EntityUid))
            {
                RaiseNetworkEvent(new ExamineSystemMessages.ExamineInfoResponseMessage(
                    request.EntityUid, _entityOutOfRangeMessage, knowTarget: false), channel);
                return;
            }

            SortedSet<Verb>? verbs = null;
            if (request.GetVerbs)
                verbs = _verbSystem.GetLocalVerbs(request.EntityUid, playerEnt, typeof(ExamineVerb));

            var text = GetExamineText(request.EntityUid, player.AttachedEntity);
            RaiseNetworkEvent(new ExamineSystemMessages.ExamineInfoResponseMessage(request.EntityUid, text, verbs?.ToList()), channel);
        }
    }
}
