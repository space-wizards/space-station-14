using Content.Shared.Medical.SuitSensor;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Medical.CrewMonitoring;

[RegisterComponent]
[Access(typeof(CrewMonitoringConsoleSystem))]
public sealed partial class CrewMonitoringConsoleComponent : Component
{
    /// <summary>
    ///     List of all currently connected sensors to this console.
    /// </summary>
    public Dictionary<string, SuitSensorStatus> ConnectedSensors = new();

    /// <summary>
    ///     Whether this console shows conscious crew listings or not. If false, will exclusively track dead or crit crew.
    /// </summary>
    [DataField]
    public bool ShowConsciousListings = true;

    /// <summary>
    ///     Departments to show on this console. Useful for, IE, a security-only monitor for the brigmedic.
    /// </summary>
    [DataField]
    public List<ProtoId<DepartmentPrototype>> TrackedDepartments { get; set; } = default!;

    /// <summary>
    ///     Specific jobs to show on this console. Useful for, IE, a prisoner-only monitor for the Warden.
    /// </summary>
    [DataField]
    public List<ProtoId<JobPrototype>> TrackedJobs { get; set; } = [];

    /// <summary>
    ///     After what time sensor consider to be lost.
    /// </summary>
    [DataField("sensorTimeout"), ViewVariables(VVAccess.ReadWrite)]
    public float SensorTimeout = 10f;
}
