using System.Collections.Generic;
using Content.Shared.Atmos.Monitor.Components;
using Robust.Shared.GameObjects;

namespace Content.Shared.Atmos.Monitor.Systems
{
    public class AirAlarmDataSystem : EntitySystem
    {
        public void UpdateDeviceData(EntityUid uid, string addr, IAtmosDeviceData data, AirAlarmDataComponent? alarmData = null)
        {
            if (!Resolve(uid, ref alarmData)) return;

            alarmData.DeviceData[addr] = data;
            alarmData.Dirty();

            RaiseLocalEvent(uid, new AirAlarmDeviceDataUpdateEvent(addr));
        }

        public void UpdateAlarmMode(EntityUid uid, AirAlarmMode mode, AirAlarmDataComponent? alarmData = null)
        {
            if (!Resolve(uid, ref alarmData)) return;

            alarmData.CurrentMode = mode;
            alarmData.Dirty();

            RaiseLocalEvent(uid, new AirAlarmSetModeEvent());
        }

        public void UpdateAlarmThreshold(EntityUid uid, AtmosAlarmThreshold threshold, AtmosMonitorThresholdType type, Gas? gas = null, AirAlarmDataComponent? alarmData = null)
        {
            if (!Resolve(uid, ref alarmData)) return;

            switch (type)
            {
                case AtmosMonitorThresholdType.Pressure:
                    alarmData.PressureThreshold = threshold;
                    break;
                case AtmosMonitorThresholdType.Temperature:
                    alarmData.TemperatureThreshold = threshold;
                    break;
                case AtmosMonitorThresholdType.Gas:
                    if (gas == null) return;
                    alarmData.GasThresholds[(Gas) gas] = threshold;
                    break;
            }

            alarmData.Dirty();

            RaiseLocalEvent(uid, new AirAlarmSetThresholdEvent(threshold, type, gas));
        }
    }

    public class AirAlarmSetThresholdEvent : EntityEventArgs
    {
        public AtmosAlarmThreshold Threshold { get; }
        public AtmosMonitorThresholdType Type { get; }
        public Gas? Gas { get; }

        public AirAlarmSetThresholdEvent(AtmosAlarmThreshold threshold, AtmosMonitorThresholdType type, Gas? gas = null)
        {
            Threshold = threshold;
            Type = type;
            Gas = gas;
        }
    }

    public class AirAlarmSetModeEvent : EntityEventArgs
    {}

    public class AirAlarmDeviceDataUpdateEvent : EntityEventArgs
    {
        public string Address { get; }

        public AirAlarmDeviceDataUpdateEvent(string addr)
        {
            Address = addr;
        }
    }

    public class AirAlarmDataUpdateEvent : EntityEventArgs
    {}
}
