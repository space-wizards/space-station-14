using Content.Shared.Medical.SuitSensor;

namespace Content.Server.Medical.CrewMonitoring;

[AutoGenerateComponentPause] // DS14
[RegisterComponent]
[Access(typeof(CrewMonitoringConsoleSystem))]
public sealed partial class CrewMonitoringConsoleComponent : Component
{
    /// <summary>
    ///     List of all currently connected sensors to this console.
    /// </summary>
    public Dictionary<string, SuitSensorStatus> ConnectedSensors = new();

    /// <summary>
    ///     After what time sensor consider to be lost.
    /// </summary>
    [DataField("sensorTimeout"), ViewVariables(VVAccess.ReadWrite)]
    public float SensorTimeout = 10f;

    // DS14-start
    [DataField]
    [AutoPausedField]
    public TimeSpan NextSound = TimeSpan.Zero;

    [DataField]
    public TimeSpan SoundInterval = TimeSpan.FromSeconds(10);

    public CrewMonitoringConsolePingMode CurrentPingMode = CrewMonitoringConsolePingMode.Severe;
    // DS14-end
}

// DS14-start
public enum CrewMonitoringConsolePingMode
{
    Severe,
    Critical,
    Dead,
    Disabled
}
// DS14-end
