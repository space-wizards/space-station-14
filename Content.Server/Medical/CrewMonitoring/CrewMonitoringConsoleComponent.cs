using Content.Shared.Medical.SuitSensor;

namespace Content.Server.Medical.CrewMonitoring
{
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

        /// <summary>
        ///     Whether the direction arrows in the monitor UI should snap the nearest diagonal or cardinal direction, or whether they should point exactly towards the target.
        /// </summary>
        [DataField("snap"), ViewVariables(VVAccess.ReadWrite)]
        public bool Snap = true;

        /// <summary>
        ///     Minimum distance before the monitor direction indicator stops pointing towards the target and instead
        ///     shows an icon indicating that the target is "here". Does not affect the displayed coordinates.
        /// </summary>
        [DataField("precision"), ViewVariables(VVAccess.ReadWrite)]
        public float Precision = 10f;
    }
}
