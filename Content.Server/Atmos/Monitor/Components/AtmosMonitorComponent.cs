using Content.Shared.Atmos;
using Content.Shared.Atmos.Monitor;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Atmos.Monitor.Components;

[RegisterComponent]
public sealed class AtmosMonitorComponent : Component
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
    [ViewVariables]
    public bool NetEnabled = true;

    [DataField("temperatureThreshold", customTypeSerializer: (typeof(PrototypeIdSerializer<AtmosAlarmThreshold>)))]
    public readonly string? TemperatureThresholdId;

    [ViewVariables]
    public AtmosAlarmThreshold? TemperatureThreshold;

    [DataField("pressureThreshold", customTypeSerializer: (typeof(PrototypeIdSerializer<AtmosAlarmThreshold>)))]
    public readonly string? PressureThresholdId;

    [ViewVariables]
    public AtmosAlarmThreshold? PressureThreshold;

    // monitor fire - much different from temperature
    // since there's events for fire, setting this to true
    // will make the atmos monitor act like a smoke detector,
    // immediately signalling danger if there's a fire
    [DataField("monitorFire")]
    public bool MonitorFire = false;

    // really messy but this is parsed at runtime after
    // prototypes are initialized, there's no
    // way without implementing a new
    // type serializer
    [DataField("gasThresholds")]
    public Dictionary<Gas, string>? GasThresholdIds;

    [ViewVariables]
    public Dictionary<Gas, AtmosAlarmThreshold>? GasThresholds;

    // Stores a reference to the gas on the tile this is on.
    [ViewVariables]
    public GasMixture? TileGas;

    // Stores the last alarm state of this alarm.
    [ViewVariables]
    public AtmosAlarmType LastAlarmState = AtmosAlarmType.Normal;

    [ViewVariables] public HashSet<AtmosMonitorThresholdType> TrippedThresholds = new();

    /// <summary>
    ///     Registered devices in this atmos monitor. Alerts will be sent directly
    ///     to these devices.
    /// </summary>
    [ViewVariables] public HashSet<string> RegisteredDevices = new();
}
