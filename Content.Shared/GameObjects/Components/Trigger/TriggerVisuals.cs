using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Triggers
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
