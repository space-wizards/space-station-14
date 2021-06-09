#nullable enable
using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Disposal
{
    [Serializable, NetSerializable]
    public enum DisposalTubeVisuals
    {
        VisualState
    }

    [Serializable, NetSerializable]
    public enum DisposalTubeVisualState
    {
        Free = 0,
        Anchored,
        Broken,
    }
}
