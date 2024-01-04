using Content.Server.Popups;
using Content.Server.Speech.Muting;
using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Content.Shared.Alert;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.Abilities.Mime
{
    public sealed class MimePowersSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly AlertsSystem _alertsSystem = default!;
        [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
        [Dependency] private readonly TurfSystem _turf = default!;
        [Dependency] private readonly IMapManager _mapMan = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;
        [Dependency] private readonly IGameTiming _timing = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MimePowersComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<MimePowersComponent, InvisibleWallActionEvent>(OnInvisibleWall);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            // Queue to track whether mimes can retake vows yet

            var query = EntityQueryEnumerator<MimePowersComponent>();
            while (query.MoveNext(out var uid, out var mime))
            {
                if (!mime.VowBroken || mime.ReadyToRepent)
                    continue;

                if (_timing.CurTime < mime.VowRepentTime)
                    continue;

                mime.ReadyToRepent = true;
                _popupSystem.PopupEntity(Loc.GetString("mime-ready-to-repent"), uid, uid);
            }
        }

        private void OnComponentInit(EntityUid uid, MimePowersComponent component, ComponentInit args)
        {
            EnsureComp<MutedComponent>(uid);
            _alertsSystem.ShowAlert(uid, AlertType.VowOfSilence);
            _actionsSystem.AddAction(uid, ref component.InvisibleWallActionEntity, component.InvisibleWallAction, uid);
        }

        /// <summary>
        /// Creates an invisible wall in a free space after some checks.
        /// </summary>
        private void OnInvisibleWall(EntityUid uid, MimePowersComponent component, InvisibleWallActionEvent args)
        {
            if (!component.Enabled)
                return;

            if (_container.IsEntityOrParentInContainer(uid))
                return;

            var xform = Transform(uid);
            // Get the tile in front of the mime
            var offsetValue = xform.LocalRotation.ToWorldVec();
            var coords = xform.Coordinates.Offset(offsetValue).SnapToGrid(EntityManager, _mapMan);
            var tile = coords.GetTileRef(EntityManager, _mapMan);
            if (tile == null)
                return;

            // Check there are no walls there
            if (_turf.IsTileBlocked(tile.Value, CollisionGroup.Impassable))
            {
                _popupSystem.PopupEntity(Loc.GetString("mime-invisible-wall-failed"), uid, uid);
                return;
            }

            // Check there are no mobs there
            foreach (var entity in _lookupSystem.GetLocalEntitiesIntersecting(tile.Value, 0f))
            {
                if (HasComp<MobStateComponent>(entity) && entity != uid)
                {
                    _popupSystem.PopupEntity(Loc.GetString("mime-invisible-wall-failed"), uid, uid);
                    return;
                }
            }
            _popupSystem.PopupEntity(Loc.GetString("mime-invisible-wall-popup", ("mime", uid)), uid);
            // Make sure we set the invisible wall to despawn properly
            Spawn(component.WallPrototype, _turf.GetTileCenter(tile.Value));
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
            RemComp<MutedComponent>(uid);
            _alertsSystem.ClearAlert(uid, AlertType.VowOfSilence);
            _alertsSystem.ShowAlert(uid, AlertType.VowBroken);
            _actionsSystem.RemoveAction(uid, mimePowers.InvisibleWallActionEntity);
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
                _popupSystem.PopupEntity(Loc.GetString("mime-not-ready-repent"), uid, uid);
                return;
            }

            mimePowers.Enabled = true;
            mimePowers.ReadyToRepent = false;
            mimePowers.VowBroken = false;
            AddComp<MutedComponent>(uid);
            _alertsSystem.ClearAlert(uid, AlertType.VowBroken);
            _alertsSystem.ShowAlert(uid, AlertType.VowOfSilence);
            _actionsSystem.AddAction(uid, ref mimePowers.InvisibleWallActionEntity, mimePowers.InvisibleWallAction, uid);
        }
    }
}
