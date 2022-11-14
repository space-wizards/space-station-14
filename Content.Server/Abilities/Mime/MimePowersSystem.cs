using Content.Server.Popups;
using Content.Server.Coordinates.Helpers;
using Content.Shared.Speech;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Physics;
using Content.Shared.Doors.Components;
using Content.Shared.Maps;
using Content.Shared.MobState.Components;
using Robust.Shared.Player;
using Robust.Shared.Physics;
using Robust.Shared.Timing;

namespace Content.Server.Abilities.Mime
{
    public sealed class MimePowersSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly AlertsSystem _alertsSystem = default!;

        [Dependency] private readonly IGameTiming _timing = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MimePowersComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<MimePowersComponent, SpeakAttemptEvent>(OnSpeakAttempt);
            SubscribeLocalEvent<MimePowersComponent, InvisibleWallActionEvent>(OnInvisibleWall);
        }
        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            // Queue to track whether mimes can retake vows yet
            foreach (var mime in EntityQuery<MimePowersComponent>())
            {
                if (!mime.VowBroken || mime.ReadyToRepent)
                    continue;

                if (_timing.CurTime < mime.VowRepentTime)
                    continue;

                mime.ReadyToRepent = true;
                _popupSystem.PopupEntity(Loc.GetString("mime-ready-to-repent"), mime.Owner, Filter.Entities(mime.Owner));
            }
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

        /// <summary>
        /// Creates an invisible wall in a free space after some checks.
        /// </summary>
        private void OnInvisibleWall(EntityUid uid, MimePowersComponent component, InvisibleWallActionEvent args)
        {
            if (!component.Enabled)
                return;

            var xform = Transform(uid);
            // Get the tile in front of the mime
            var offsetValue = xform.LocalRotation.ToWorldVec().Normalized;
            var coords = xform.Coordinates.Offset(offsetValue).SnapToGrid(EntityManager);
            // Check there are no walls or mobs there
            foreach (var entity in coords.GetEntitiesInTile())
            {
                IPhysBody? physics = null; // We use this to check if it's impassable
                if ((HasComp<MobStateComponent>(entity) && entity != uid) || // Is it a mob?
                    ((Resolve(entity, ref physics, false) && (physics.CollisionLayer & (int) CollisionGroup.Impassable) != 0) // Is it impassable?
                    &&  !(TryComp<DoorComponent>(entity, out var door) && door.State != DoorState.Closed))) // Is it a door that's open and so not actually impassable?
                {
                    _popupSystem.PopupEntity(Loc.GetString("mime-invisible-wall-failed"), uid, Filter.Entities(uid));
                    return;
                }
            }
            _popupSystem.PopupEntity(Loc.GetString("mime-invisible-wall-popup", ("mime", uid)), uid, Filter.Pvs(uid));
            // Make sure we set the invisible wall to despawn properly
            Spawn(component.WallPrototype, coords);
            // Handle args so cooldown works
            args.Handled = true;
        }

        /// <summary>
        /// Break this mime's vow to not speak.
        /// </summary>
        public void BreakVow(EntityUid uid, MimePowersComponent? mimePowers = null)
        {
            if (!Resolve(uid, ref mimePowers))
                return;

            if (mimePowers.VowBroken)
                return;

            mimePowers.Enabled = false;
            mimePowers.VowBroken = true;
            mimePowers.VowRepentTime = _timing.CurTime + mimePowers.VowCooldown;
            _alertsSystem.ClearAlert(uid, AlertType.VowOfSilence);
            _alertsSystem.ShowAlert(uid, AlertType.VowBroken);
            _actionsSystem.RemoveAction(uid, mimePowers.InvisibleWallAction);
        }

        /// <summary>
        /// Retake this mime's vow to not speak.
        /// </summary>
        public void RetakeVow(EntityUid uid, MimePowersComponent? mimePowers = null)
        {
            if (!Resolve(uid, ref mimePowers))
                return;

            if (!mimePowers.ReadyToRepent)
            {
                _popupSystem.PopupEntity(Loc.GetString("mime-not-ready-repent"), uid, Filter.Entities(uid));
                return;
            }

            mimePowers.Enabled = true;
            mimePowers.ReadyToRepent = false;
            mimePowers.VowBroken = false;
            _alertsSystem.ClearAlert(uid, AlertType.VowBroken);
            _alertsSystem.ShowAlert(uid, AlertType.VowOfSilence);
            _actionsSystem.AddAction(uid, mimePowers.InvisibleWallAction, uid);
        }
    }

    public sealed class InvisibleWallActionEvent : InstantActionEvent {}
}
