using System;
using System.Collections.Generic;
using Content.Shared.Atmos.Monitor.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Log;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Monitor.Systems
{
    // Note: sendEvent (bool) exists to avoid sending events server side at the moment
    // when an update is called for data that needs to be synced to clients.
    // This is a little awkward and tangly, and probably needs to be
    // aggressively refactored
    public class AirAlarmDataSystem : EntitySystem
    {
        public override void Initialize()
        {
            SubscribeLocalEvent<AirAlarmDataComponent, ComponentGetState>(OnDataGetState);
            SubscribeLocalEvent<AirAlarmDataComponent, ComponentHandleState>(OnDataHandleState);
        }

        // server side (mostly), except for when
        // this is initialized
        private void OnDataGetState(EntityUid uid, AirAlarmDataComponent data, ref ComponentGetState state)
        {
            Logger.DebugS("AirAlarmData", "Attempting to grab state now.");
            var alarmState = new AirAlarmDataComponentState
            {
                AirData = data.AirData
            };

            if (data.DirtyMode)
            {
                Logger.DebugS("AirAlarmData", "Dirty air alarm mode detected.");
                alarmState.DirtyMode = true;
                alarmState.CurrentMode = data.CurrentMode;
                data.DirtyMode = false;
            }

            if (data.DirtyThresholds.Count != 0)
            {
                Logger.DebugS("AirAlarmData", "Dirty thresholds detected.");
                alarmState.DirtyThresholds = data.DirtyThresholds;
                foreach (var threshold in data.DirtyThresholds)
                {
                    Logger.DebugS("AirAlarmData", $"Sending {threshold} as part of state.");
                    switch (threshold)
                    {
                        case AtmosMonitorThresholdType.Pressure:
                            alarmState.PressureThreshold = data.PressureThreshold;
                            break;
                        case AtmosMonitorThresholdType.Temperature:
                            alarmState.TemperatureThreshold = data.TemperatureThreshold;
                            break;
                        case AtmosMonitorThresholdType.Gas:
                            alarmState.GasThresholds = data.GasThresholds;
                            break;
                    }
                }
                data.DirtyThresholds.Clear();
            }

            if (data.DirtyDevices.Count != 0)
            {
                alarmState.DirtyDevices = data.DirtyDevices;
                var dirtyDevices = new Dictionary<string, IAtmosDeviceData>();
                foreach (var deviceAddr in data.DirtyDevices)
                    dirtyDevices.Add(deviceAddr, data.DeviceData[deviceAddr]);

                alarmState.DeviceData = dirtyDevices;
                data.DirtyDevices.Clear();
            }

            Logger.DebugS("AirAlarmData", "Setting state.");
            state.State = alarmState;
        }

        // client side
        private void OnDataHandleState(EntityUid uid, AirAlarmDataComponent data, ref ComponentHandleState state)
        {
            if (state.Current is not AirAlarmDataComponentState currentState) return;

            Logger.DebugS("AirAlarmData", "Handling state.");
            Logger.DebugS("AirAlarmData", $"Current state info:");
            Logger.DebugS("AirAlarmData", $"moded:{currentState.DirtyMode}");
            if (currentState.DirtyThresholds != null)
                foreach (var threshold in currentState.DirtyThresholds)
                    Logger.DebugS("AirAlarmData", $"thresd:{threshold}");
            if (currentState.DirtyDevices != null)
                foreach (var device in currentState.DirtyDevices)
                    Logger.DebugS("AirAlarmData", $"devd:{device}");

            // UpdateAirData(uid, currentState.AirData);
            data.AirData = currentState.AirData;

            if (currentState.DirtyMode)
            {
                Logger.DebugS("AirAlarmData", "Dirty mode detected.");
                // UpdateAlarmMode(uid, currentState.CurrentMode, false, data);
                data.CurrentMode = currentState.CurrentMode;
                data.DirtyMode = true;
            }

            if (currentState.DirtyThresholds != null)
            {
                Logger.DebugS("AirAlarmData", "Dirty thresholds detected");
                foreach (var threshold in currentState.DirtyThresholds)
                {
                    data.DirtyThresholds.Add(threshold);
                    Logger.DebugS("AirAlarmData", $"Handling dirty threshold ${threshold}");
                    switch (threshold)
                    {
                        case AtmosMonitorThresholdType.Pressure:
                            // UpdateAlarmThreshold(uid, currentState.PressureThreshold!, threshold, null, false, data);
                            data.PressureThreshold = currentState.PressureThreshold!;
                            break;
                        case AtmosMonitorThresholdType.Temperature:
                            // UpdateAlarmThreshold(uid, currentState.TemperatureThreshold!, threshold, null, false, data);
                            data.TemperatureThreshold = currentState.TemperatureThreshold!;
                            break;
                        case AtmosMonitorThresholdType.Gas:
                            foreach (var (gas, gasThreshold) in currentState.GasThresholds!)
                                // UpdateAlarmThreshold(uid, gasThreshold, threshold, gas, false, data);
                                data.GasThresholds[gas] = gasThreshold;
                            break;
                    }
                }
            }

            if (currentState.DirtyDevices != null)
            {
                var deviceData = new Dictionary<string, IAtmosDeviceData>(currentState.DeviceData!);
                foreach (var addr in currentState.DirtyDevices)
                {
                    data.DeviceData[addr] = currentState.DeviceData![addr];
                    data.DirtyDevices.Add(addr);
                }
            }
        }

        /*
        public void UpdateDeviceData(EntityUid uid, string addr, IAtmosDeviceData data, bool sendEvent = false, AirAlarmDataComponent? alarmData = null)
        {
            if (!Resolve(uid, ref alarmData)) return;

            alarmData.DeviceData[addr] = data;
            alarmData.DirtyDevices.Add(addr);
            alarmData.Dirty();

            if (sendEvent)
                RaiseLocalEvent(uid, new AirAlarmDeviceDataUpdateEvent(addr));
        }

        public void UpdateAlarmMode(EntityUid uid, AirAlarmMode mode, bool sendEvent = false, AirAlarmDataComponent? alarmData = null)
        {
            if (!Resolve(uid, ref alarmData)) return;

            Logger.DebugS("AirAlarmData", "Attempting to update alarm mode now.");
            alarmData.CurrentMode = mode;
            alarmData.DirtyMode = true;
            alarmData.Dirty();

            if (sendEvent)
                RaiseLocalEvent(uid, new AirAlarmSetModeEvent());
        }

        public void UpdateAlarmThreshold(EntityUid uid, AtmosAlarmThreshold threshold, AtmosMonitorThresholdType type, Gas? gas = null, bool sendEvent = false, AirAlarmDataComponent? alarmData = null)
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

            alarmData.DirtyThresholds.Add(type);
            alarmData.Dirty();

            if (sendEvent)
                RaiseLocalEvent(uid, new AirAlarmSetThresholdEvent(threshold, type, gas));
        }

        public void UpdateAirData(EntityUid uid, AirAlarmAirData update, AirAlarmDataComponent? data = null)
        {
            if (!Resolve(uid, ref data)) return;

            Logger.DebugS("AirAlarmData", "Attempting to update air data now.");

            // TODO: Lerping? (sounds like a noise a furry would make lmao)
            data.AirData = update;

            data.Dirty();
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
