#nullable enable
using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Singularity
{
    [NetSerializable, Serializable]
    public enum RadiationCollectorVisuals
    {
        VisualState
    }

    [NetSerializable, Serializable]
    public enum RadiationCollectorVisualState
    {
        Active,
        Activating,
        Deactivating,
        Deactive
    }
}
