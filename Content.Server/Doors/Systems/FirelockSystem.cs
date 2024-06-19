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
            Dirty(uid, component);
        }

        public override void Update(float frameTime)
        {
            _accumulatedTicks += 1;
            if (_accumulatedTicks < UpdateInterval)
                return;

            _accumulatedTicks = 0;

            // Go through all of the firelocks and re-close them if they've been pried open.
            var query = EntityQueryEnumerator<FirelockComponent, DoorComponent>();
            while (query.MoveNext(out var uid, out var firelock, out var door))
            {
                if (firelock.IsLocked && _gameTiming.CurTime > firelock.EmergencyCloseCooldown)
                {
                    EmergencyPressureStop(uid, firelock, door);
                }
            }
        }

        private void OnAtmosAlarm(EntityUid uid, FirelockComponent component, AtmosAlarmEvent args)
        {
            if (!this.IsPowered(uid, EntityManager))
                return;

            if (!TryComp<DoorComponent>(uid, out var doorComponent))
                return;

            if (args.AlarmType == AtmosAlarmType.Normal || args.AlarmType == AtmosAlarmType.Warning)
            {
                component.IsLocked = false;
                _doorSystem.TryOpen(uid);
            }
            else if (args.AlarmType == AtmosAlarmType.Danger)
            {
                component.IsLocked = true;
                EmergencyPressureStop(uid, component, doorComponent);
            }
            Dirty(uid, component);
        }
    }
}
