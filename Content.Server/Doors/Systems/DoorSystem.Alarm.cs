using Content.Server.Atmos.Components;
using Content.Server.Atmos.Monitor.Systems;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Robust.Server.GameObjects;

namespace Content.Server.Doors.Systems
{
    public sealed partial class DoorSystem
    {
        private void InitializeFirelock()
        {
            SubscribeLocalEvent<DoorAlarmComponent, AtmosAlarmEvent>(OnAtmosAlarm);
        }

        // TODO: It'd be nice if this was just a door alarm event?
        private void OnAtmosAlarm(Entity<DoorAlarmComponent> doorAlarm, ref AtmosAlarmEvent args)
        {
            switch (args.AlarmType)
            {
                case AtmosAlarmType.Normal:
                    if (!doorAlarm.Comp.AlarmSources.Remove(args.Alarm) || doorAlarm.Comp.AlarmSources.Count > 0)
                        break;

                    SetAlarm(doorAlarm, false);

                    break;
                case AtmosAlarmType.Danger:
                    if (!doorAlarm.Comp.AlarmSources.Add(args.Alarm))
                        return;

                    TriggerAlarm((doorAlarm, doorAlarm.Comp));

                    break;
            }
        }

    }
}
