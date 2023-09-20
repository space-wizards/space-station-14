using Content.Shared.Atmos;
using Content.Shared.Atmos.Monitor;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Server.Atmos.Monitor.Components;

[RegisterComponent]
public sealed partial class AtmosMonitorComponent : Component
{
    // Whether this monitor can send alarms,
    // or recieve atmos command events.
    //
    // Useful for wires; i.e., pulsing a monitor wire
    // will make it send an alert, and cutting
    // it will make it so that alerts are no longer
    // sent/receieved.
    //
    // Note that this cancels every single network
    // event, including ones that may not be
    // related to atmos monitor events.
    [DataField("netEnabled")]
    public bool NetEnabled = true;

    [DataField("temperatureThresholdId", customTypeSerializer: (typeof(PrototypeIdSerializer<AtmosAlarmThresholdPrototype>)))]
    public string? TemperatureThresholdId;

    [DataField("temperatureThreshold")]
    public AtmosAlarmThreshold? TemperatureThreshold;

    [DataField("pressureThresholdId", customTypeSerializer: (typeof(PrototypeIdSerializer<AtmosAlarmThresholdPrototype>)))]
    public string? PressureThresholdId;

    [DataField("pressureThreshold")]
    public AtmosAlarmThreshold? PressureThreshold;

    // monitor fire - much different from temperature
    // since there's events for fire, setting this to true
    // will make the atmos monitor act like a smoke detector,
    // immediately signalling danger if there's a fire
    [DataField("monitorFire")]
    public bool MonitorFire = false;

    [DataField("gasThresholdPrototypes",
        customTypeSerializer:typeof(PrototypeIdValueDictionarySerializer<Gas, AtmosAlarmThresholdPrototype>))]
    public Dictionary<Gas, string>? GasThresholdPrototypes;

    [DataField("gasThresholds")]
    public Dictionary<Gas, AtmosAlarmThreshold>? GasThresholds;

    // Stores a reference to the gas on the tile this is on.
    [ViewVariables]
    public GasMixture? TileGas;

    // Stores the last alarm state of this alarm.
    [DataField("lastAlarmState")]
    public AtmosAlarmType LastAlarmState = AtmosAlarmType.Normal;

    [DataField("trippedThresholds")]
    public HashSet<AtmosMonitorThresholdType> TrippedThresholds = new();

    /// <summary>
    ///     Registered devices in this atmos monitor. Alerts will be sent directly
    ///     to these devices.
    /// </summary>
    [DataField("registeredDevices")]
    public HashSet<string> RegisteredDevices = new();
}
