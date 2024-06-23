using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Monitor.Systems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Shuttles.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server.Doors.Systems
{
    public sealed class FirelockSystem : SharedFirelockSystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly SharedDoorSystem _doorSystem = default!;
        [Dependency] private readonly AtmosphereSystem _atmosSystem = default!;
        [Dependency] private readonly SharedMapSystem _mapping = default!;

        private const int UpdateInterval = 30;
        private int _accumulatedTicks;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<FirelockComponent, AtmosAlarmEvent>(OnAtmosAlarm);
            SubscribeLocalEvent<FirelockComponent, PowerChangedEvent>(PowerChanged);
        }

        private void PowerChanged(EntityUid uid, FirelockComponent component, ref PowerChangedEvent args)
        {
            component.Powered = args.Powered;
            UpdateDoorState(uid, component);
        }

        public override void Update(float frameTime)
        {
            _accumulatedTicks += 1;
            if (_accumulatedTicks < UpdateInterval)
                return;

            _accumulatedTicks = 0;

            var airtightQuery = GetEntityQuery<AirtightComponent>();
            var xformQuery = GetEntityQuery<TransformComponent>();
            var query = EntityQueryEnumerator<FirelockComponent, DoorComponent>();
            while (query.MoveNext(out var uid, out var firelock, out var door))
            {
                // only bother to check pressure on doors that are some variation of closed.
                if (door.State != DoorState.Closed
                    && door.State != DoorState.Welded
                    && door.State != DoorState.Denying
                    && airtightQuery.TryGetComponent(uid, out var airtight)
                    && xformQuery.TryGetComponent(uid, out var xform))
                {
                    var pressure = CheckDiffPressure(uid, firelock, xform, airtight, airtightQuery);
                    firelock.Pressure = pressure;
                }

                // Always run this to re-close firelocks after they've been pried open.
                UpdateDoorState(uid, firelock);
            }
        }

        private void OnAtmosAlarm(EntityUid uid, FirelockComponent component, AtmosAlarmEvent args)
        {
            if (args.AlarmType == AtmosAlarmType.Normal)
                component.ExtLocked = false;
            else if (args.AlarmType == AtmosAlarmType.Danger)
                component.ExtLocked = true;
            UpdateDoorState(uid, component);
        }

        private void UpdateDoorState(EntityUid uid, FirelockComponent component)
        {
            Dirty(uid, component);
            if (!this.IsPowered(uid, EntityManager) || !TryComp<DoorComponent>(uid, out var door))
                return;

            if (component.IsLocked)
            {
                if (component.EmergencyCloseCooldown == null || _gameTiming.CurTime > component.EmergencyCloseCooldown)
                    EmergencyPressureStop(uid, component, door);
            }
            else if (door.State == DoorState.Closed)
            {
                _doorSystem.TryOpen(uid);
            }
        }

        public bool CheckDiffPressure(
        EntityUid uid,
        FirelockComponent firelock,
        TransformComponent xform,
        AirtightComponent airtight,
        EntityQuery<AirtightComponent> airtightQuery)
        {
            if (!airtight.AirBlocked)
                return false;

            if (TryComp(uid, out DockingComponent? dock) && dock.Docked)
            {
                // Currently docking automatically opens the doors. But maybe in future, check the pressure difference before opening doors?
                return false;
            }

            if (!HasComp<GridAtmosphereComponent>(xform.ParentUid))
                return false;

            var grid = Comp<MapGridComponent>(xform.ParentUid);
            var pos = _mapping.CoordinatesToTile(xform.ParentUid, grid, xform.Coordinates);
            var minPressure = float.MaxValue;
            var maxPressure = float.MinValue;
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

            var gasses = _atmosSystem.GetTileMixtures(xform.ParentUid, xform.MapUid, tiles);
            if (gasses == null)
                return false;

            for (var i = 0; i < count; i++)
            {
                var gas = gasses[i];
                var dir = directions[i];
                var adjacentPos = tiles[i];

                if (gas != null)
                {
                    // Is there some airtight entity blocking this direction? If yes, don't include this direction in the
                    // pressure differential
                    if (HasAirtightBlocker(_mapping.GetAnchoredEntities(xform.ParentUid, grid, adjacentPos), dir.GetOpposite(), airtightQuery))
                        continue;

                    var p = gas.Pressure;
                    minPressure = Math.Min(minPressure, p);
                    maxPressure = Math.Max(maxPressure, p);
                }

                holdingPressure |= maxPressure - minPressure > firelock.PressureThreshold;

                if (holdingPressure)
                    return holdingPressure;
            }

            if (airtight.AirBlockedDirection == AtmosDirection.All)
                return holdingPressure;

            var local = gasses[count];
            if (local != null)
            {
                var p = local.Pressure;
                minPressure = Math.Min(minPressure, p);
                maxPressure = Math.Max(maxPressure, p);
            }
            else
            {
                minPressure = Math.Min(minPressure, 0);
                maxPressure = Math.Max(maxPressure, 0);
            }

            holdingPressure |= maxPressure - minPressure > firelock.PressureThreshold;

            return holdingPressure;
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
