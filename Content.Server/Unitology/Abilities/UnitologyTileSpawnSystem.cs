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
using Content.Shared.Necroobelisk.Components;

namespace Content.Server.Abilities.Unitolog
{
    public sealed class UnitologTileSpawnSystem : EntitySystem
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
            SubscribeLocalEvent<UnitologTileSpawnComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<UnitologTileSpawnComponent, TileSpawnActionEvent>(OnInvisibleWall);
        }

        private void OnComponentInit(EntityUid uid, UnitologTileSpawnComponent component, ComponentInit args)
        {
            _alertsSystem.ShowAlert(uid, AlertType.VowOfSilence);
            _actionsSystem.AddAction(uid, ref component.TileSpawnActionEntity, component.TileSpawnAction, uid);
        }

        /// <summary>
        /// Creates an invisible wall in a free space after some checks.
        /// </summary>
        private void OnInvisibleWall(EntityUid uid, UnitologTileSpawnComponent component, TileSpawnActionEvent args)
        {

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
                _popupSystem.PopupEntity(Loc.GetString("Тут нельзя разместить тентаклю"), uid, uid);
                return;
            }

            Spawn(component.WallPrototype, _turf.GetTileCenter(tile.Value));
            // Handle args so cooldown works
            args.Handled = true;
        }

    }
}
