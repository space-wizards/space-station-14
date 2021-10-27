using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Atmos.Monitor.Components
{
    // Due to the mass of data in an air alarm,
    // this is required for sane, quick syncing
    // without sending massive packets over
    // a BoundUserInterfaceState, or turning
    // a lot of server-side components into
    // networked components (eurgh).
    //
    // AirAlarmSystem will update this on its own
    // loop, telling an AirAlarmComponent to
    // also send an update to the BoundUserInterface
    // so that the interface in question can
    // sync its data with this component.
    //
    // The reasoning behind this is that a lot
    // of this data is server-side only, which
    // means that the client can't innately access
    // this. This is the bridge.
    //
    // AirAlarmDataSystem will handle the syncing of
    // UI to data, including BoundUserInt
    [RegisterComponent]
    [NetworkedComponent]
    public class AirAlarmDataComponent : Component
    {
        public override string Name => "AirAlarmData";

        [ViewVariables] public float? Pressure { get; set; }
        [ViewVariables] public float? Temperature { get; set; }
        [ViewVariables] public float? TotalMoles { get; set; }
        [ViewVariables] public AirAlarmMode CurrentMode { get; set; }
        [ViewVariables] public AtmosMonitorAlarmType AlarmState { get; set; }
        [ViewVariables] public AtmosAlarmThreshold PressureThreshold { get; set; } = new();
        [ViewVariables] public AtmosAlarmThreshold TemperatureThreshold { get; set; } = new();

        [ViewVariables] public Dictionary<string, IAtmosDeviceData> DeviceData { get; } = new();
        // might be needed, might not
        // sync errors need to be implemented first
        [ViewVariables] public Dictionary<string, bool> DeviceDataErrors { get; } = new();
        [ViewVariables] public Dictionary<Gas, float> Gases { get; } = new();
        [ViewVariables] public Dictionary<Gas, AtmosAlarmThreshold> GasThresholds { get; } = new();
    }

    [Serializable, NetSerializable]
    public sealed class AirAlarmDataComponentState : ComponentState
    {
        public float? Pressure { get; set; }
        public float? Temperature { get; set; }
        public float? TotalMoles { get; set; }
        public AirAlarmMode CurrentMode { get; set; }
        public AtmosMonitorAlarmType AlarmState { get; set; }
        public AtmosAlarmThreshold PressureThreshold { get; set; } = new();
        public AtmosAlarmThreshold TemperatureThreshold { get; set; } = new();

        public Dictionary<string, IAtmosDeviceData> DeviceData { get; }
        // might be needed, might not
        // sync errors need to be implemented first
        // public Dictionary<string, bool> DeviceDataErrors { get; }
        public Dictionary<Gas, float> Gases { get; }
        public Dictionary<Gas, AtmosAlarmThreshold> GasThresholds { get; }

        public AirAlarmDataComponentState(float? pressure,
            float? temperature,
            float? totalMoles,
            AirAlarmMode currentMode,
            AtmosMonitorAlarmType alarmState,
            AtmosAlarmThreshold pressureThreshold,
            AtmosAlarmThreshold temperatureThreshold,
            Dictionary<string, IAtmosDeviceData> deviceData,
            Dictionary<Gas, float> gases,
            Dictionary<Gas, AtmosAlarmThreshold> gasThresholds)
        {
            Pressure = pressure;
            Temperature = temperature;
            TotalMoles = totalMoles;
            CurrentMode = currentMode;
            AlarmState = alarmState;
            PressureThreshold = pressureThreshold;
            TemperatureThreshold = temperatureThreshold;
            DeviceData = deviceData;
            Gases = gases;
            GasThresholds = gasThresholds;
        }

    }
}
