using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Content.Shared.Examine
{
    public sealed class ExamineStatsEvent : EntityEventArgs
    {
        public FormattedMessage Message = new FormattedMessage();
    }

    [UsedImplicitly]
    public sealed class CondensedExamineSystem : EntitySystem
    {
        [Dependency] private readonly ExamineSystemShared _examineSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<CondensedExamineComponent, GetVerbsEvent<ExamineVerb>>(OnExamineVerb);
        }

        public void OnExamineVerb(EntityUid uid, CondensedExamineComponent component, GetVerbsEvent<ExamineVerb> args)
        {
            if (args.CanAccess != component.CanAccess || args.CanInteract != component.CanInteract)
                return;

            var ev = new ExamineStatsEvent() { };
            RaiseLocalEvent(uid, ev);

            if (ev.Message.IsEmpty)
                return;

            var msg = new FormattedMessage();
            msg.AddMarkup(Loc.TryGetString(component.FirstLine, out var firstLine) ? firstLine : component.FirstLine);
            msg.AddMessage(ev.Message);

            var verb = new ExamineVerb()
            {
                Act = () =>
                {
                    _examineSystem.SendExamineTooltip(args.User, uid, msg, false, false);
                },
                Text = Loc.TryGetString(component.Text, out var text) ? text : component.Text,
                Message = Loc.TryGetString(component.Message, out var message) ? message : component.Message,
                Category = VerbCategory.Examine,
                IconTexture = component.Icon
            };

            args.Verbs.Add(verb);
        }
    }
}
