using Content.Server.Atmos.Monitor.Components;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Shared.Atmos.Monitor;
using Robust.Server.GameObjects;

namespace Content.Server.Atmos.Monitor.Systems
{
    public sealed class AtmosAlarmableSystem : EntitySystem
    {
        [Dependency] private readonly AppearanceSystem _appearance = default!;
        public override void Initialize()
        {
            SubscribeLocalEvent<AtmosAlarmableComponent, DeviceNetworkPacketEvent>(OnPacketRecv);
        }

        private void OnPacketRecv(EntityUid uid, AtmosAlarmableComponent component, DeviceNetworkPacketEvent args)
        {
            if (component.IgnoreAlarms) return;

            if (!EntityManager.TryGetComponent(uid, out DeviceNetworkComponent? netConn))
                return;

            if (args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? cmd)
                && cmd == AtmosMonitorSystem.AtmosMonitorAlarmCmd)
            {
                // does it have a state & network max state?
                // does it have a source?
                // and can this be alarmed by the source?
                // if so, raise an alarm
                if (args.Data.TryGetValue(DeviceNetworkConstants.CmdSetState, out AtmosMonitorAlarmType state)
                    && args.Data.TryGetValue(AtmosMonitorSystem.AtmosMonitorAlarmNetMax, out AtmosMonitorAlarmType netMax)
                    && args.Data.TryGetValue(AtmosMonitorSystem.AtmosMonitorAlarmSrc, out string? source)
                    && component.AlarmedByPrototypes.Contains(source))
                {
                    component.LastAlarmState = state;
                    component.HighestNetworkState = netMax;
                    UpdateAppearance(uid, netMax);
                    RaiseLocalEvent(component.Owner, new AtmosMonitorAlarmEvent(state, netMax), true);
                }
            }
        }

        private void UpdateAppearance(EntityUid uid, AtmosMonitorAlarmType alarm)
        {
            _appearance.SetData(uid, AtmosMonitorVisuals.AlarmType, alarm);
        }
    }
}
