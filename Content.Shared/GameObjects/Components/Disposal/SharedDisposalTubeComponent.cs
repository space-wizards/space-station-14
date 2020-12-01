using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Disposal
{
    [Serializable, NetSerializable]
    public enum DisposalTubeVisuals : byte
    {
        VisualState
    }

    [Serializable, NetSerializable]
    public enum DisposalTubeVisualState : byte
    {
        Free = 0,
        Anchored,
        Broken,
    }
}
