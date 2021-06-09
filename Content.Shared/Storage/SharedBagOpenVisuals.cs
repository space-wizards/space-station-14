#nullable enable
using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components
{
    [Serializable, NetSerializable]
    public enum SharedBagOpenVisuals : byte
    {
        BagState,
    }
    
    [Serializable, NetSerializable]
    public enum SharedBagState : byte
    {
        Open,
        Close,
    }
}
