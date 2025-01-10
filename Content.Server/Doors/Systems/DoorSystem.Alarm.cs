using Content.Server.Atmos.Components;
using Content.Server.Atmos.Monitor.Systems;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Doors.Components;
using Robust.Server.GameObjects;

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
            SubscribeLocalEvent<DoorAlarmComponent, AtmosAlarmEvent>(OnAtmosAlarm);
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

            var doorQuery = EntityQueryEnumerator<DoorAlarmComponent, DoorComponent>();
            while (doorQuery.MoveNext(out var uid, out var firelock, out var door))
            {
                // only bother to check pressure on doors that are some variation of closed.
                if (
                    door.State is not (DoorState.Closed or DoorState.WeldedClosed or DoorState.Denying) ||
                    !_airtightQuery.TryGetComponent(uid, out _) ||
                    !_xformQuery.TryGetComponent(uid, out _) ||
                    !_appearanceQuery.TryGetComponent(uid, out var appearance)
                )
                    continue;

                _appearance.SetData(uid, DoorVisuals.ClosedLights, firelock.IsTriggered, appearance);

                if (_pointLightQuery.TryComp(uid, out var pointLight))
                    _pointLight.SetEnabled(uid, firelock.IsActive, pointLight);

                Dirty(uid, firelock);
            }
        }

        // TODO: It'd be nice if this was just a door alarm event?
        private void OnAtmosAlarm(Entity<DoorAlarmComponent> doorAlarm, ref AtmosAlarmEvent args)
        {
            switch (args.AlarmType)
            {
                case AtmosAlarmType.Normal:
                    if (!doorAlarm.Comp.AlarmSources.Remove(args.Alarm) || doorAlarm.Comp.AlarmSources.Count > 0)
                        break;

                    doorAlarm.Comp.IsTriggered = false;

                    if (TryComp<DoorComponent>(doorAlarm, out var door))
                        TryOpen((doorAlarm, door));

                    break;
                case AtmosAlarmType.Danger:
                    if (!doorAlarm.Comp.AlarmSources.Add(args.Alarm))
                        return;

                    TriggerAlarm((doorAlarm, doorAlarm.Comp));

                    break;
                case AtmosAlarmType.Invalid:
                case AtmosAlarmType.Warning:
                case AtmosAlarmType.Emagged:
                default:
                    break;
            }
        }
    }
}
