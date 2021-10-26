using System.Collections.Generic;
using Robust.Shared.GameObjects;

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
    public class AirAlarmDataComponent : Component
    {
        public override string Name => "AirAlarmData";

        public float? Pressure { get; set; }
        public float? Temperature { get; set; }
        public float? TotalMoles { get; set; }
        public AirAlarmMode CurrentMode { get; set; }
        public AtmosMonitorAlarmType AlarmState { get; set; }
        public AtmosAlarmThreshold PressureThreshold { get; set; } = new();
        public AtmosAlarmThreshold TemperatureThreshold { get; set; } = new();

        public Dictionary<string, IAtmosDeviceData> DeviceData { get; } = new();
        // might be needed, might not
        // sync errors need to be implemented first
        public Dictionary<string, bool> DeviceDataErrors { get; } = new();
        public Dictionary<Gas, float> Gases { get; } = new();
        public Dictionary<Gas, AtmosAlarmThreshold> GasThresholds { get; } = new();
    }
}
