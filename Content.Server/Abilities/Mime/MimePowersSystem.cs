using Content.Shared.Speech;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Server.Coordinates.Helpers;
using Robust.Shared.Player;

namespace Content.Server.Abilities.Mime
{
    public sealed class MimePowersSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly AlertsSystem _alertsSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MimePowersComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<MimePowersComponent, SpeakAttemptEvent>(OnSpeakAttempt);
            SubscribeLocalEvent<MimePowersComponent, InvisibleWallActionEvent>(OnInvisibleWall);
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

        private void OnInvisibleWall(EntityUid uid, MimePowersComponent component, InvisibleWallActionEvent args)
        {
            var xform = Transform(uid);

            var offsetValue = xform.LocalRotation.ToWorldVec().Normalized;
            var coords = xform.Coordinates.Offset(offsetValue);
            EntityManager.SpawnEntity("WallSolid", coords.SnapToGrid());
        }
    }

    public sealed class InvisibleWallActionEvent : InstantActionEvent {}
}
