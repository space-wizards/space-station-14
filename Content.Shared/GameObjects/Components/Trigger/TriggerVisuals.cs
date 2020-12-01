using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Trigger
{
    [NetSerializable]
    [Serializable]
    public enum TriggerVisuals : byte
    {
        VisualState,
    }

    [NetSerializable]
    [Serializable]
    public enum TriggerVisualState : byte
    {
        Primed,
        Unprimed,
    }
}
