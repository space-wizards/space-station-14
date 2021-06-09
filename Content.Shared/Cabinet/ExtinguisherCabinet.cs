#nullable enable
using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components
{
    [Serializable, NetSerializable]
    public enum ItemCabinetVisuals : byte
    {
        IsOpen,
        ContainsItem
    }
}
