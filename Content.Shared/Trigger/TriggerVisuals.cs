#nullable enable
using System;
using Robust.Shared.Serialization;

namespace Content.Shared.Trigger
{
    [NetSerializable]
    [Serializable]
    public enum TriggerVisuals
    {
        VisualState,
    }

    [NetSerializable]
    [Serializable]
    public enum TriggerVisualState
    {
        Primed,
        Unprimed,
    }
}
