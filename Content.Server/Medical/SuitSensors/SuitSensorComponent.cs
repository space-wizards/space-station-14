using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Server.Medical.SuitSensors
{
    [RegisterComponent]
    [Friend(typeof(SuitSensorSystem))]
    public class SuitSensorComponent : Component
    {
        public override string Name => "SuitSensor";

        public SuitSensorMode Mode;
    }

    public enum SuitSensorMode : byte
    {
        SensorOff = 0,
        SensorBinary = 1,
        SensorVitals = 2,
        SensorCords = 3
    }
}
