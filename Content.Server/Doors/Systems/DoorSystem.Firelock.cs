using Content.Server.Atmos.Components;
using Content.Server.Atmos.Monitor.Systems;
using Content.Server.Power.EntitySystems;
using Content.Server.Shuttles.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Doors.Components;
using Content.Shared.Power;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;

namespace Content.Server.Doors.Systems
{
    public sealed partial class DoorSystem
    {
        private const int UpdateInterval = 30;
        private int _accumulatedTicks;

        private EntityQuery<AirtightComponent> _airtightQuery;
        private EntityQuery<AppearanceComponent> _appearanceQuery;
        private EntityQuery<TransformComponent> _xformQuery;
        private EntityQuery<PointLightComponent> _pointLightQuery;

        private void InitializeFirelock()
        {
            SubscribeLocalEvent<FirelockComponent, AtmosAlarmEvent>(OnAtmosAlarm);
            SubscribeLocalEvent<FirelockComponent, PowerChangedEvent>(PowerChanged);
        }

        private void PowerChanged(Entity<FirelockComponent> firelock, ref PowerChangedEvent args)
        {
            // TODO this should REALLLLY not be door specific appearance thing.
            _appearance.SetData(firelock, DoorVisuals.Powered, args.Powered);
            firelock.Comp.Powered = args.Powered;
            Dirty(firelock);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            _accumulatedTicks += 1;
            if (_accumulatedTicks < UpdateInterval)
                return;

            _accumulatedTicks = 0;

            _airtightQuery = GetEntityQuery<AirtightComponent>();
            _appearanceQuery = GetEntityQuery<AppearanceComponent>();
            _xformQuery = GetEntityQuery<TransformComponent>();
            _pointLightQuery = GetEntityQuery<PointLightComponent>();

            var doorQuery = EntityQueryEnumerator<FirelockComponent, DoorComponent>();
            while (doorQuery.MoveNext(out var uid, out var firelock, out var door))
            {
                // only bother to check pressure on doors that are some variation of closed.
                if (
                    door.State is not (DoorState.Closed or DoorState.WeldedClosed or DoorState.Denying) ||
                    !_airtightQuery.TryGetComponent(uid, out var airtight) ||
                    !_xformQuery.TryGetComponent(uid, out var xform) ||
                    !_appearanceQuery.TryGetComponent(uid, out var appearance)
                )
                    continue;

                (firelock.Pressure, firelock.Temperature) =
                    CheckPressureAndFire((uid, firelock), xform, airtight, _airtightQuery);
                _appearance.SetData(uid, DoorVisuals.ClosedLights, firelock.IsLocked, appearance);

                Dirty(uid, firelock);

                if (_pointLightQuery.TryComp(uid, out var pointLight))
                    _pointLight.SetEnabled(uid, firelock.IsLocked, pointLight);
            }
        }

        private void OnAtmosAlarm(Entity<FirelockComponent> firelock, ref AtmosAlarmEvent args)
        {
            if (!this.IsPowered(firelock, EntityManager) || !TryComp<DoorComponent>(firelock, out var door))
                return;

            switch (args.AlarmType)
            {
                case AtmosAlarmType.Normal:
                    if (door.State == DoorState.Closed)
                        _doorSystem.TryOpen((firelock, door));

                    return;
                case AtmosAlarmType.Danger:
                    EmergencyPressureStop((firelock, firelock.Comp, door));

                    return;
                case AtmosAlarmType.Invalid:
                case AtmosAlarmType.Warning:
                case AtmosAlarmType.Emagged:
                default:
                    return;
            }
        }

        public (bool Pressure, bool Fire) CheckPressureAndFire(
            Entity<FirelockComponent> firelock,
            TransformComponent xform,
            AirtightComponent airtight,
            EntityQuery<AirtightComponent> airtightQuery)
        {
            if (!airtight.AirBlocked)
                return (false, false);

            if (TryComp(firelock, out DockingComponent? dock) && dock.Docked)
            {
                // Currently docking automatically opens the doors. But maybe in future, check the pressure difference before opening doors?
                return (false, false);
            }

            if (!HasComp<GridAtmosphereComponent>(xform.ParentUid))
                return (false, false);

            var grid = Comp<MapGridComponent>(xform.ParentUid);
            var pos = _mapping.CoordinatesToTile(xform.ParentUid, grid, xform.Coordinates);
            var minPressure = float.MaxValue;
            var maxPressure = float.MinValue;
            var minTemperature = float.MaxValue;
            var maxTemperature = float.MinValue;
            var holdingFire = false;
            var holdingPressure = false;

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
                var dir = (AtmosDirection)(1 << i);
                if (!airtight.AirBlockedDirection.HasFlag(dir))
                    continue;

                directions.Add(dir);
                tiles.Add(pos.Offset(dir));
            }

            // May also have to consider pressure on the same tile as the firelock.
            var count = tiles.Count;
            if (airtight.AirBlockedDirection != AtmosDirection.All)
                tiles.Add(pos);

            var gasses = _atmosSystem.GetTileMixtures(xform.ParentUid, xform.MapUid, tiles);
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
                    if (HasAirtightBlocker(_mapping.GetAnchoredEntities(xform.ParentUid, grid, adjacentPos),
                            dir.GetOpposite(),
                            airtightQuery))
                        continue;

                    var p = gas.Pressure;
                    minPressure = Math.Min(minPressure, p);
                    maxPressure = Math.Max(maxPressure, p);
                    minTemperature = Math.Min(minTemperature, gas.Temperature);
                    maxTemperature = Math.Max(maxTemperature, gas.Temperature);
                }

                holdingPressure |= maxPressure - minPressure > firelock.Comp.PressureThreshold;
                holdingFire |= maxTemperature - minTemperature > firelock.Comp.TemperatureThreshold;

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

            holdingPressure |= maxPressure - minPressure > firelock.Comp.PressureThreshold;
            holdingFire |= maxTemperature - minTemperature > firelock.Comp.TemperatureThreshold;

            return (holdingPressure, holdingFire);
        }

        private bool HasAirtightBlocker(IEnumerable<EntityUid> enumerable,
            AtmosDirection dir,
            EntityQuery<AirtightComponent> airtightQuery)
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
