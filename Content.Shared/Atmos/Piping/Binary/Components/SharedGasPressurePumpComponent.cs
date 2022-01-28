using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Piping.Binary.Components
{
    [Serializable, NetSerializable]
    public enum GasPressurePumpUiKey
    {
        Key,
    }

    [Serializable, NetSerializable]
    public class GasPressurePumpBoundUserInterfaceState : BoundUserInterfaceState
    {
        public string PumpLabel { get; }
        public float OutputPressure { get; }
        public bool Enabled { get; }

        public GasPressurePumpBoundUserInterfaceState(string pumpLabel, float outputPressure, bool enabled)
        {
            PumpLabel = pumpLabel;
            OutputPressure = outputPressure;
            Enabled = enabled;
        }
    }

    [Serializable, NetSerializable]
    public class GasPressurePumpToggleStatusMessage : BoundUserInterfaceMessage
    {
        public bool Enabled { get; }

        public GasPressurePumpToggleStatusMessage(bool enabled)
        {
            Enabled = enabled;
        }
    }

    [Serializable, NetSerializable]
    public class GasPressurePumpChangeOutputPressureMessage : BoundUserInterfaceMessage
    {
        public float Pressure { get; }

        public GasPressurePumpChangeOutputPressureMessage(float pressure)
        {
            Pressure = pressure;
        }
    }
}
