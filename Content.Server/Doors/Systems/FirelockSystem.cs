using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Monitor.Systems;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Shuttles.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Popups;
using Microsoft.Extensions.Options;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;

namespace Content.Server.Doors.Systems
{
    public sealed class FirelockSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedDoorSystem _doorSystem = default!;
        [Dependency] private readonly AtmosAlarmableSystem _atmosAlarmable = default!;
        [Dependency] private readonly AtmosphereSystem _atmosSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

        private static float _visualUpdateInterval = 0.5f;
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
            SubscribeLocalEvent<FirelockComponent, ComponentStartup>(UpdateVisuals);
            SubscribeLocalEvent<FirelockComponent, PowerChangedEvent>(PowerChanged);
        }

        private void PowerChanged(EntityUid uid, FirelockComponent component, ref PowerChangedEvent args)
        {
            // TODO this should REALLLLY not be door specific appearance thing.
            _appearance.SetData(uid, DoorVisuals.Powered, args.Powered);
        }

        #region Visuals
        private void UpdateVisuals(EntityUid uid, FirelockComponent component, EntityEventArgs args) => UpdateVisuals(uid, component);

        public override void Update(float frameTime)
        {
            _accumulatedFrameTime += frameTime;
            if (_accumulatedFrameTime < _visualUpdateInterval)
                return;

            _accumulatedFrameTime -= _visualUpdateInterval;

            var airtightQuery = GetEntityQuery<AirtightComponent>();
            var appearanceQuery = GetEntityQuery<AppearanceComponent>();
            var xformQuery = GetEntityQuery<TransformComponent>();

            foreach (var (firelock, door) in EntityQuery<FirelockComponent, DoorComponent>())
            {
                // only bother to check pressure on doors that are some variation of closed.
                if (door.State != DoorState.Closed
                    && door.State != DoorState.Welded
                    && door.State != DoorState.Denying)
                {
                    continue;
                }

                var uid = door.Owner;
                if (airtightQuery.TryGetComponent(uid, out var airtight)
                    && xformQuery.TryGetComponent(uid, out var xform)
                    && appearanceQuery.TryGetComponent(uid, out var appearance))
                {
                    var (fire, pressure) = CheckPressureAndFire(uid, firelock, xform, airtight, airtightQuery);
                    _appearance.SetData(uid, DoorVisuals.ClosedLights, fire || pressure, appearance);
                }
            }
        }

        private void UpdateVisuals(EntityUid uid,
            FirelockComponent? firelock = null,
            DoorComponent? door = null,
            AirtightComponent? airtight = null,
            AppearanceComponent? appearance = null,
            TransformComponent? xform = null)
        {
            if (!Resolve(uid, ref door, ref appearance, false))
                return;

            // only bother to check pressure on doors that are some variation of closed.
            if (door.State != DoorState.Closed
                && door.State != DoorState.Welded
                && door.State != DoorState.Denying)
            {
                _appearance.SetData(uid, DoorVisuals.ClosedLights, false, appearance);
                return;
            }

            var query = GetEntityQuery<AirtightComponent>();
            if (!Resolve(uid, ref firelock, ref airtight, ref appearance, ref xform, false) || !query.Resolve(uid, ref airtight, false))
                return;

            var (fire, pressure) = CheckPressureAndFire(uid, firelock, xform, airtight, query);
            _appearance.SetData(uid, DoorVisuals.ClosedLights, fire || pressure, appearance);
        }
        #endregion

        public bool EmergencyPressureStop(EntityUid uid, FirelockComponent? firelock = null, DoorComponent? door = null)
        {
            if (!Resolve(uid, ref firelock, ref door))
                return false;

            if (door.State == DoorState.Open)
            {
                if (_doorSystem.TryClose(door.Owner, door))
                {
                    return _doorSystem.OnPartialClose(door.Owner, door);
                }
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
                    uid, args.User, PopupType.MediumCaution);
            }
            else if (state.Pressure)
            {
                _popupSystem.PopupEntity(Loc.GetString("firelock-component-is-holding-pressure-message"),
                    uid, args.User, PopupType.MediumCaution);
            }

            if (state.Fire || state.Pressure)
                args.PryTimeModifier *= component.LockedPryTimeModifier;
        }

        private void OnUpdateState(EntityUid uid, FirelockComponent component, DoorStateChangedEvent args)
        {
            var ev = new BeforeDoorAutoCloseEvent();
            RaiseLocalEvent(uid, ev);
            UpdateVisuals(uid, component, args);
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
            if (!this.IsPowered(uid, EntityManager))
                return;

            if (!TryComp<DoorComponent>(uid, out var doorComponent))
                return;

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

        public bool IsHoldingPressureOrFire(EntityUid uid, FirelockComponent firelock)
        {
            var result = CheckPressureAndFire(uid, firelock);
            return result.Pressure || result.Fire;
        }

        public (bool Pressure, bool Fire) CheckPressureAndFire(EntityUid uid, FirelockComponent firelock)
        {
            var query = GetEntityQuery<AirtightComponent>();
            if (query.TryGetComponent(uid, out AirtightComponent? airtight))
                return CheckPressureAndFire(uid, firelock, Transform(uid), airtight, query);
            return (false, false);
        }

        public (bool Pressure, bool Fire) CheckPressureAndFire(
        EntityUid uid,
        FirelockComponent firelock,
        TransformComponent xform,
        AirtightComponent airtight,
        EntityQuery<AirtightComponent> airtightQuery)
        {
            if (!airtight.AirBlocked)
                return (false, false);

            if (TryComp(uid, out DockingComponent? dock) && dock.Docked)
            {
                // Currently docking automatically opens the doors. But maybe in future, check the pressure difference before opening doors?
                return (false, false);
            }

            if (!TryComp(xform.ParentUid, out GridAtmosphereComponent? gridAtmosphere))
                return (false, false);

            var grid = Comp<MapGridComponent>(xform.ParentUid);
            var pos = grid.CoordinatesToTile(xform.Coordinates);
            var minPressure = float.MaxValue;
            var maxPressure = float.MinValue;
            var minTemperature = float.MaxValue;
            var maxTemperature = float.MinValue;
            bool holdingFire = false;
            bool holdingPressure = false;

            // We cannot simply use `_atmosSystem.GetAdjacentTileMixtures` because of how the `includeBlocked` option
            // works, we want to ignore the firelock's blocking, while including blockers on other tiles.
            // GetAdjacentTileMixtures also ignores empty/non-existent tiles, which we don't want. Additionally, for
            // edge-fire locks, we only want to enumerate over a single directions. So AFAIK there is no nice way of
            // achieving all this using existing atmos functions, and the functionality is too specialized to bother
            // adding new public atmos system functions.

            List<Vector2i> tiles = new(4);
            List<AtmosDirection> directions = new(4);
            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var dir = (AtmosDirection) (1 << i);
                if (airtight.AirBlockedDirection.HasFlag(dir))
                {
                    directions.Add(dir);
                    tiles.Add(pos.Offset(dir));
                }
            }

            // May also have to consider pressure on the same tile as the firelock.
            var count = tiles.Count;
            if (airtight.AirBlockedDirection != AtmosDirection.All)
                tiles.Add(pos);

            var gasses = _atmosSystem.GetTileMixtures(gridAtmosphere.Owner, xform.MapUid, tiles);
            if (gasses == null)
                return (false, false);

            for (var i = 0; i < count; i++)
            {
                var gas = gasses[i];
                var dir = directions[i];
                var adjacentPos = tiles[i];

                if (gas != null)
                {
                    // Is there some airtight entity blocking this direction? If yes, don't include this direction in the
                    // pressure differential
                    if (HasAirtightBlocker(grid.GetAnchoredEntities(adjacentPos), dir.GetOpposite(), airtightQuery))
                        continue;

                    var p = gas.Pressure;
                    minPressure = Math.Min(minPressure, p);
                    maxPressure = Math.Max(maxPressure, p);
                    minTemperature = Math.Min(minTemperature, gas.Temperature);
                    maxTemperature = Math.Max(maxTemperature, gas.Temperature);
                }

                holdingPressure |= maxPressure - minPressure > firelock.PressureThreshold;
                holdingFire |= maxTemperature - minTemperature > firelock.TemperatureThreshold;

                if (holdingPressure && holdingFire)
                    return (holdingPressure, holdingFire);
            }

            if (airtight.AirBlockedDirection == AtmosDirection.All)
                return (holdingPressure, holdingFire);

            var local = gasses[count];
            if (local != null)
            {
                var p = local.Pressure;
                minPressure = Math.Min(minPressure, p);
                maxPressure = Math.Max(maxPressure, p);
                minTemperature = Math.Min(minTemperature, local.Temperature);
                maxTemperature = Math.Max(maxTemperature, local.Temperature);
            }
            else
            {
                minPressure = Math.Min(minPressure, 0);
                maxPressure = Math.Max(maxPressure, 0);
                minTemperature = Math.Min(minTemperature, 0);
                maxTemperature = Math.Max(maxTemperature, 0);
            }

            holdingPressure |= maxPressure - minPressure > firelock.PressureThreshold;
            holdingFire |= maxTemperature - minTemperature > firelock.TemperatureThreshold;

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
