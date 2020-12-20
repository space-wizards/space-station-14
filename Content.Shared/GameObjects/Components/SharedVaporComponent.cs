using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components
{
    [Serializable, NetSerializable]
    public enum VaporVisuals
    {
        Rotation,
        Color,
        State,
    }
}
