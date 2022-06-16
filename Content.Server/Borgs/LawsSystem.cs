using Content.Shared.Actions;
using Content.Server.Chat;

namespace Content.Server.Borgs
{
    public sealed class LawsSystem : EntitySystem
    {
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<LawsComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<LawsComponent, StateLawsActionEvent>(OnStateLaws);
        }

        private void OnInit(EntityUid uid, LawsComponent component, ComponentInit args)
        {
            _actionsSystem.AddAction(uid, component.StateLawsAction, uid);
        }

        private void OnStateLaws(EntityUid uid, LawsComponent component, StateLawsActionEvent args)
        {
            int i = 0;
            foreach (var law in component.Laws)
            {
                var message = ("Law " + i +": " + law);
                _chat.TrySendInGameICMessage(uid, message, InGameICChatType.Speak, false);
                i++;
            }
        }
    }
    public sealed class StateLawsActionEvent : InstantActionEvent {}
}
