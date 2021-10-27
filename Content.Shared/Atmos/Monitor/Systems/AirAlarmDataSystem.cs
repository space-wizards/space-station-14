using System;
using System.Collections.Generic;
using Content.Shared.Atmos.Monitor.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Log;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Monitor.Systems
{
    public class AirAlarmDataSystem : EntitySystem
    {
        public override void Initialize()
        {
            SubscribeLocalEvent<AirAlarmDataComponent, ComponentGetState>(OnDataGetState);
            SubscribeLocalEvent<AirAlarmDataComponent, ComponentHandleState>(OnDataHandleState);
        }

        private void OnDataGetState(EntityUid uid, AirAlarmDataComponent data, ref ComponentGetState state)
        {
            Logger.DebugS("AirAlarmData", "Attempting to grab state now.");
            state.State = new AirAlarmDataComponentState(data.Pressure, data.Temperature, data.TotalMoles, data.CurrentMode, data.AlarmState, data.PressureThreshold, data.TemperatureThreshold, data.DeviceData, data.Gases, data.GasThresholds);
        }

        private void OnDataHandleState(EntityUid uid, AirAlarmDataComponent data, ref ComponentHandleState state)
        {
            if (state.Current is not AirAlarmDataComponentState currentState) return;

            var update = new AirAlarmData
            {
                Pressure = currentState.Pressure,
                Temperature = currentState.Temperature,
                TotalMoles = currentState.TotalMoles,
            };

            UpdateAirData(uid, update);
        }

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

        public void UpdateAirData(EntityUid uid, AirAlarmData update, AirAlarmDataComponent? data = null)
        {
            if (!Resolve(uid, ref data)) return;

            Logger.DebugS("AirAlarmData", "Attempting to update data now.");

            // TODO: Lerping? (sounds like a noise a furry would make lmao)
            data.Pressure = update.Pressure;
            data.Temperature = update.Temperature;
            data.TotalMoles = update.TotalMoles;

            foreach (var (gas, amount) in update.Gases)
                if (!data.Gases.TryAdd(gas, amount))
                    data.Gases[gas] = amount;

            data.Dirty();
        }

    }

    [Serializable, NetSerializable]
    public class AirAlarmData
    {
        public float? Pressure { get; set; }
        public float? Temperature { get; set; }
        public float? TotalMoles { get; set; }

        public Dictionary<Gas, float> Gases { get; } = new();

        /*
        public AirAlarmData(float? pressure, float? temperature, float? moles, Dictionary<Gas, float> gases)
        {
            Pressure = pressure;
            Temperature = temperature;
            TotalMoles = moles;
            Gases = gases;
        }
        */
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
