using Content.Shared.Speech;
using Content.Shared.Popups;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Robust.Shared.Player;

namespace Content.Shared.Abilities.Mime
{
    public sealed class MimePowersSystem : EntitySystem
    {
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly AlertsSystem _alertsSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MimePowersComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<MimePowersComponent, SpeakAttemptEvent>(OnSpeakAttempt);
        }

        private void OnComponentInit(EntityUid uid, MimePowersComponent component, ComponentInit args)
        {
            _actionsSystem.AddAction(uid, component.InvisibleWallAction, uid);
            _alertsSystem.ShowAlert(uid, AlertType.VowOfSilence);
        }
        private void OnSpeakAttempt(EntityUid uid, MimePowersComponent component, SpeakAttemptEvent args)
        {
            if (!component.Enabled)
                return;

            _popupSystem.PopupEntity(Loc.GetString("mime-cant-speak"), uid, Filter.Entities(uid));
            args.Cancel();
        }
    }

    public sealed class InvisibleWallActionEvent : InstantActionEvent {}
}
