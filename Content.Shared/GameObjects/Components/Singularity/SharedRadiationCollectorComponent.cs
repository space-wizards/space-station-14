using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Singularity
{
    [NetSerializable, Serializable]
    public enum RadiationCollectorVisuals : byte
    {
        VisualState
    }

    [NetSerializable, Serializable]
    public enum RadiationCollectorVisualState : byte
    {
        Active,
        Activating,
        Deactivating,
        Deactive
    }
}
