using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Storage
{
    [Serializable, NetSerializable]
    public enum WallStorageStatus
    {
        Full,
        Empty,
    }
}
