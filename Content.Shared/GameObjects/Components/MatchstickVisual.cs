using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components
{
    [Serializable, NetSerializable]
    public enum MatchstickVisual : byte
    {
        Igniting,
    }
}
