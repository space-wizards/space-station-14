using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Piping.Trinary.Components
{
    [Serializable, NetSerializable]
    public enum GasMixerUiKey
    {
        Key,
    }

    [Serializable, NetSerializable]
    public class GasMixerBoundUserInterfaceState : BoundUserInterfaceState
    {
        public string MixerLabel { get; }
        public float OutputPressure { get; }
        public bool Enabled { get; }

        public float NodeOne { get; }

        public GasMixerBoundUserInterfaceState(string mixerLabel, float outputPressure, bool enabled, float nodeOne)
        {
            MixerLabel = mixerLabel;
            OutputPressure = outputPressure;
            Enabled = enabled;
            NodeOne = nodeOne;
        }
    }

    [Serializable, NetSerializable]
    public class GasMixerToggleStatusMessage : BoundUserInterfaceMessage
    {
        public bool Enabled { get; }

        public GasMixerToggleStatusMessage(bool enabled)
        {
            Enabled = enabled;
        }
    }

    [Serializable, NetSerializable]
    public class GasMixerChangeOutputPressureMessage : BoundUserInterfaceMessage
    {
        public float Pressure { get; }

        public GasMixerChangeOutputPressureMessage(float pressure)
        {
            Pressure = pressure;
        }
    }

    [Serializable, NetSerializable]
    public class GasMixerChangeNodePercentageMessage : BoundUserInterfaceMessage
    {
        public float NodeOne { get; }

        public GasMixerChangeNodePercentageMessage(float nodeOne)
        {
            NodeOne = nodeOne;
        }
    }
}
