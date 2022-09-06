using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Monitor.Components;
using Content.Server.Atmos.Monitor.Systems;
using Content.Server.Doors.Components;
using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Shuttles.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Server.Doors.Systems
{
    public sealed class FirelockSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedDoorSystem _doorSystem = default!;
        [Dependency] private readonly AtmosAlarmableSystem _atmosAlarmable = default!;
        [Dependency] private readonly AtmosphereSystem _atmosSystem = default!;
        [Dependency] private readonly AirtightSystem _airtightSystem = default!;

        private static float _visualUpdateInterval = 0.2f;
        private float _accumulatedFrameTime;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<FirelockComponent, BeforeDoorOpenedEvent>(OnBeforeDoorOpened);
            SubscribeLocalEvent<FirelockComponent, DoorGetPryTimeModifierEvent>(OnDoorGetPryTimeModifier);
            SubscribeLocalEvent<FirelockComponent, DoorStateChangedEvent>(OnUpdateState);

            SubscribeLocalEvent<FirelockComponent, BeforeDoorAutoCloseEvent>(OnBeforeDoorAutoclose);
            SubscribeLocalEvent<FirelockComponent, AtmosAlarmEvent>(OnAtmosAlarm);

            // Visuals
            SubscribeLocalEvent<FirelockComponent, MapInitEvent>(UpdateVisuals);
            SubscribeLocalEvent<FirelockComponent, DoorStateChangedEvent>(UpdateVisuals);
            SubscribeLocalEvent<FirelockComponent, PowerChangedEvent>(UpdateVisuals);
        }

        #region Visuals
        private void UpdateVisuals(EntityUid uid, FirelockComponent component, EntityEventArgs args) => UpdateVisuals(uid);

        public override void Update(float frameTime)
        {
            _accumulatedFrameTime += frameTime;
            if (_accumulatedFrameTime < _visualUpdateInterval)
                return;

            _accumulatedFrameTime -= _visualUpdateInterval;

            var powerQuery = GetEntityQuery<ApcPowerReceiverComponent>();
            var airtightQuery = GetEntityQuery<AirtightComponent>();

            foreach (var (firelock, door, appearance, xform) in EntityQuery<FirelockComponent, DoorComponent, AppearanceComponent, TransformComponent>())
            {
                UpdateVisuals(door.Owner, firelock, door, null, appearance, xform, powerQuery, airtightQuery);
            }
        }

        private void UpdateVisuals(EntityUid uid,
            FirelockComponent? firelock = null,
            DoorComponent? door = null,
            AirtightComponent? airtight = null,
            AppearanceComponent? appearance = null,
            TransformComponent? xform = null,
            EntityQuery<ApcPowerReceiverComponent>? powerQuery = null,
            EntityQuery<AirtightComponent>? airtightQuery = null)
        {
            if (!Resolve(uid, ref door, ref appearance, false))
                return;

            // only bother to check pressure on doors that are some variation of closed.
            if (door.State != DoorState.Closed
                && door.State != DoorState.Welded
                && door.State != DoorState.Denying)
            {
                appearance.SetData(DoorVisuals.ClosedLights, false);
                return;
            }

            powerQuery ??= EntityManager.GetEntityQuery<ApcPowerReceiverComponent>();
            if (powerQuery.Value.TryGetComponent(uid, out var receiver) && !receiver.Powered)
            {
                appearance.SetData(DoorVisuals.ClosedLights, false);
                return;
            }

            appearance.SetData(DoorVisuals.ClosedLights,
                IsHoldingPressureOrFire(uid, firelock, xform, airtight, airtightQuery));
        }
        #endregion

        public bool EmergencyPressureStop(EntityUid uid, FirelockComponent? firelock = null, DoorComponent? door = null)
        {
            if (!Resolve(uid, ref firelock, ref door))
                return false;

            if (door.State == DoorState.Open &&
                _doorSystem.CanClose(uid, door))
            {
                _doorSystem.StartClosing(uid, door);

                // Door system also sets airtight, but only after a delay. We want it to be immediate.
                if (TryComp(uid, out AirtightComponent? airtight))
                {
                    _airtightSystem.SetAirblocked(airtight, true);
                }
                return true;
            }
            return false;
        }

        private void OnBeforeDoorOpened(EntityUid uid, FirelockComponent component, BeforeDoorOpenedEvent args)
        {
            if (!this.IsPowered(uid, EntityManager) || IsHoldingPressureOrFire(uid, component))
                args.Cancel();
        }

        private void OnDoorGetPryTimeModifier(EntityUid uid, FirelockComponent component, DoorGetPryTimeModifierEvent args)
        {
            var state = CheckPressureAndFire(uid, component);

            if (state.Fire)
            {
                _popupSystem.PopupEntity(Loc.GetString("firelock-component-is-holding-fire-message"),
                    uid, Filter.Pvs(uid, entityManager: EntityManager));
            }
            else if (state.Pressure)
            {
                _popupSystem.PopupEntity(Loc.GetString("firelock-component-is-holding-pressure-message"),
                    uid, Filter.Pvs(uid, entityManager: EntityManager));
            }

            if (state.Fire || state.Pressure)
                args.PryTimeModifier *= component.LockedPryTimeModifier;
        }

        private void OnUpdateState(EntityUid uid, FirelockComponent component, DoorStateChangedEvent args)
        {
            var ev = new BeforeDoorAutoCloseEvent();
            RaiseLocalEvent(uid, ev);
            if (ev.Cancelled)
            {
                return;
            }

            _doorSystem.SetNextStateChange(uid, component.AutocloseDelay);
        }

        private void OnBeforeDoorAutoclose(EntityUid uid, FirelockComponent component, BeforeDoorAutoCloseEvent args)
        {
            if (!this.IsPowered(uid, EntityManager))
                args.Cancel();

            // Make firelocks autoclose, but only if the last alarm type it
            // remembers was a danger. This is to prevent people from
            // flooding hallways with endless bad air/fire.
            if (component.AlarmAutoClose &&
                (_atmosAlarmable.TryGetHighestAlert(uid, out var alarm) && alarm != AtmosAlarmType.Danger || alarm == null))
                args.Cancel();
        }

        private void OnAtmosAlarm(EntityUid uid, FirelockComponent component, AtmosAlarmEvent args)
        {
            if (!TryComp<DoorComponent>(uid, out var doorComponent)) return;

            if (args.AlarmType == AtmosAlarmType.Normal || args.AlarmType == AtmosAlarmType.Warning)
            {
                if (doorComponent.State == DoorState.Closed)
                    _doorSystem.TryOpen(uid);
            }
            else if (args.AlarmType == AtmosAlarmType.Danger)
            {
                EmergencyPressureStop(uid, component, doorComponent);
            }
        }

        public bool IsHoldingPressureOrFire(
            EntityUid uid,
            FirelockComponent? firelock = null,
            TransformComponent? xform = null,
            AirtightComponent? airtight = null,
            EntityQuery<AirtightComponent>? airtightQuery = null)
        {
            var result = CheckPressureAndFire(uid, firelock, xform, airtight, airtightQuery);
            return result.Pressure || result.Fire;
        }

        public (bool Pressure, bool Fire) CheckPressureAndFire(
            EntityUid uid,
            FirelockComponent? firelock = null,
            TransformComponent? xform = null,
            AirtightComponent? airtight = null,
            EntityQuery<AirtightComponent>? airtightQuery = null)
        {
            airtightQuery ??= GetEntityQuery<AirtightComponent>();
            if (!airtightQuery.Value.Resolve(uid, ref airtight, false))
                return (false, false);

            if (!airtight.AirBlocked)
                return (false, false);

            if (!Resolve(uid, ref firelock, ref xform))
                return (false, false);

            if (TryComp(uid, out DockingComponent? dock) && dock.Docked)
            {
                // Currently docking automatically opens the doors. But maybe in future, check the pressure difference before opening doors?
                return (false, false);
            }

            if (!xform.Anchored)
                return (false, false);

            if (!TryComp(xform.ParentUid, out GridAtmosphereComponent? gridAtmosphere))
                return (false, false);

            var grid = Comp<IMapGridComponent>(xform.ParentUid).Grid;
            var pos = grid.CoordinatesToTile(xform.Coordinates);
            var minPressure = float.MaxValue;
            var maxPressure = float.MinValue;
            bool holdingFire = false;
            bool holdingPressure = false;

            // We cannot simply use `_atmosSystem.GetAdjacentTileMixtures` because of how the `includeBlocked` option
            // works, we want to ignore the firelock's blocking, while including blockers on other tiles.
            // GetAdjacentTileMixtures also ignores empty/non-existent tiles, which we don't want. Additionally, for
            // edge-fire locks, we only want to enumerate over a single directions. So AFAIK there is no nice way of
            // achieving all this using existing atmos functions, and the functionality is too specialized to bother
            // adding new public atmos system functions.

            float pressure;
            TileAtmosphere? atmos;
            // >> TileAtmosphere
            // >> You shouldn't use this directly, use <see cref="AtmosphereSystem"/> instead.
            // umm... uhh.... I'll pretend I didn't read that.
            // I just need to query both pressure and IsHotspotActive at the same time.

            // TODO redo this with planet/map atmospheres

            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var dir = (AtmosDirection) (1 << i);

                if (!airtight.AirBlockedDirection.HasFlag(dir))
                    continue;

                var adjacentPos = pos.Offset(dir);
                atmos = _atmosSystem.GetTileAtmosphere(gridAtmosphere, adjacentPos);

                if (atmos != null)
                {
                    // Is there some airtight entity blocking this direction? If yes, don't include this direction in the
                    // pressure differential
                    if (HasAirtightBlocker(grid.GetAnchoredEntities(adjacentPos), dir.GetOpposite(), airtightQuery.Value))
                        continue;

                    holdingFire |= atmos.Hotspot.Valid;
                    pressure = atmos.Air?.Pressure ?? 0;
                }
                else
                    pressure = 0;

                minPressure = Math.Min(minPressure, pressure);
                maxPressure = Math.Max(maxPressure, pressure);

                holdingPressure |= maxPressure - minPressure > firelock.PressureThreshold;

                if (holdingPressure && holdingFire)
                    return (holdingPressure, holdingFire);
            }

            // May also have to consider pressure on the same tile as the firelock.
            if (airtight.AirBlockedDirection == AtmosDirection.All)
                return (holdingPressure, holdingFire);

            atmos = _atmosSystem.GetTileAtmosphere(gridAtmosphere, pos);
            if (atmos != null)
            {
                holdingFire |= atmos.Hotspot.Valid;
                pressure = atmos.Air?.Pressure ?? 0;
            }
            else
                pressure = 0;

            minPressure = Math.Min(minPressure, pressure);
            maxPressure = Math.Max(maxPressure, pressure);
            holdingPressure |= maxPressure - minPressure > firelock.PressureThreshold;

            return (holdingPressure, holdingFire);
        }

        private bool HasAirtightBlocker(IEnumerable<EntityUid> enumerable, AtmosDirection dir, EntityQuery<AirtightComponent> airtightQuery)
        {
            foreach (var ent in enumerable)
            {
                if (!airtightQuery.TryGetComponent(ent, out var airtight) || !airtight.AirBlocked)
                    continue;

                if ((airtight.AirBlockedDirection & dir) == dir)
                    return true;
            }

            return false;
        }
    }
}
