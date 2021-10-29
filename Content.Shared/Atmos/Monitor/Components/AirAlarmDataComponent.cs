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

        // These are the 'hot' values an air alarm contains.
        // These are the ones that have to be updated by
        // the server.
        /*
        [ViewVariables] public float? Pressure { get; set; }
        [ViewVariables] public float? Temperature { get; set; }
        [ViewVariables] public float? TotalMoles { get; set; }
        [ViewVariables] public AtmosMonitorAlarmType AlarmState { get; set; }
        [ViewVariables] public Dictionary<Gas, float> Gases { get; } = new();
        */
        [ViewVariables] public AirAlarmAirData AirData;

        // These ar the user configurable values an air alarm contains.
        // These are the ones that are updated by an end user, after
        // being fetched from a server.
        [ViewVariables] public bool DirtyMode { get; set; }
        [ViewVariables] public AirAlarmMode CurrentMode { get; set; }

        // If any of these thresholds are dirty, it is added to the hash set.
        // Upon state transfer, all dirty-indicator sets should be cleared.
        [ViewVariables] public HashSet<AtmosMonitorThresholdType> DirtyThresholds { get; } = new();
        [ViewVariables] public AtmosAlarmThreshold PressureThreshold { get; set; } = new();
        [ViewVariables] public AtmosAlarmThreshold TemperatureThreshold { get; set; } = new();
        [ViewVariables] public Dictionary<Gas, AtmosAlarmThreshold> GasThresholds { get; } = new();

        [ViewVariables] public HashSet<string> DirtyDevices { get; } = new();
        [ViewVariables] public Dictionary<string, IAtmosDeviceData> DeviceData { get; set; } = new();
        // might be needed, might not
        // sync errors need to be implemented first
        [ViewVariables] public Dictionary<string, bool> DeviceDataErrors { get; } = new();
    }

    [Serializable, NetSerializable]
    public sealed class AirAlarmDataComponentState : ComponentState
    {
        // These are the 'hot' values an air alarm contains.
        // These are the ones that have to be updated by
        // the server.
        /*
        [ViewVariables] public float? Pressure { get; set; }
        [ViewVariables] public float? Temperature { get; set; }
        [ViewVariables] public float? TotalMoles { get; set; }
        [ViewVariables] public AtmosMonitorAlarmType AlarmState { get; set; }
        [ViewVariables] public Dictionary<Gas, float>? Gases;
        */

        public AirAlarmAirData AirData { get; set; }

        /*
        [ViewVariables] public IReadOnlyDictionary<Gas, float>? Gases
        {
            get => _gases;
            set { if (value != null) _gases = new Dictionary<Gas, float>(value); }
        }
        */

        // These ar the user configurable values an air alarm contains.
        // These are the ones that are updated by an end user, after
        // being fetched from a server.
        [ViewVariables] public bool DirtyMode { get; set; }
        [ViewVariables] public AirAlarmMode CurrentMode { get; set; }

        // If any of these thresholds are dirty, it is added to the hash set.
        private HashSet<AtmosMonitorThresholdType>? _dirtyThresholds;
        [ViewVariables] public IReadOnlyCollection<AtmosMonitorThresholdType>? DirtyThresholds
        {
            get => _dirtyThresholds;
            set { if (value != null) _dirtyThresholds = new HashSet<AtmosMonitorThresholdType>(value); }
        }
        [ViewVariables] public AtmosAlarmThreshold? PressureThreshold { get; set; }
        [ViewVariables] public AtmosAlarmThreshold? TemperatureThreshold { get; set; }
        [ViewVariables] public Dictionary<Gas, AtmosAlarmThreshold>? GasThresholds;
        /*
        [ViewVariables] public IReadOnlyDictionary<Gas, AtmosAlarmThreshold>? GasThresholds
        {
            get => _gasThresholds;
            set { if (value != null) _gasThresholds = new Dictionary<Gas, AtmosAlarmThreshold>(value); }
        }
        */
        private HashSet<string>? _dirtyDevices;
        public IReadOnlyCollection<string>? DirtyDevices
        {
            get => _dirtyDevices;
            set { if (value != null) _dirtyDevices = new HashSet<string>(value); }
        }
        [ViewVariables] public Dictionary<string, IAtmosDeviceData>? DeviceData;
        /*
        [ViewVariables] public IReadOnlyDictionary<string, IAtmosDeviceData>? DeviceData
        {
            get => _deviceData;
            set { if (value != null) _deviceData = new Dictionary<string, IAtmosDeviceData>(value); }
        }
        */
        // might be needed, might not
        // sync errors need to be implemented first
        [ViewVariables] public Dictionary<string, bool> DeviceDataErrors { get; } = new();

    }

    [Serializable, NetSerializable]
    public readonly struct AirAlarmAirData
    {
        public readonly float? Pressure { get; }
        public readonly float? Temperature { get; }
        public readonly float? TotalMoles { get; }
        public readonly AtmosMonitorAlarmType AlarmState { get; }

        private readonly Dictionary<Gas, float>? _gases;
        public readonly IReadOnlyDictionary<Gas, float>? Gases { get => _gases; }

        public AirAlarmAirData(float? pressure, float? temperature, float? moles, AtmosMonitorAlarmType state, Dictionary<Gas, float>? gases)
        {
            Pressure = pressure;
            Temperature = temperature;
            TotalMoles = moles;
            AlarmState = state;
            _gases = gases;
        }
    }

}
