using Robust.Shared.Serialization;
using System;

namespace Content.Shared.GameObjects.Components.Atmos
{
    [NetSerializable]
    [Serializable]
    public enum GasAnalyzerVisuals : byte
    {
        VisualState,
    }

    [NetSerializable]
    [Serializable]
    public enum GasAnalyzerVisualState : byte
    {
        Off,
        Working,
    }
}
