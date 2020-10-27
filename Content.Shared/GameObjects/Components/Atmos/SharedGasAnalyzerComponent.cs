using Robust.Shared.Serialization;
using System;

namespace Content.Shared.GameObjects.Components.Atmos
{
    [NetSerializable]
    [Serializable]
    public enum GasAnalyzerVisuals
    {
        VisualState,
    }

    [NetSerializable]
    [Serializable]
    public enum GasAnalyzerVisualState
    {
        Off,
        Working,
    }
}
