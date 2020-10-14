using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Body.Part
{
    [Serializable, NetSerializable]
    public enum BodyPartSymmetry
    {
        None = 0,
        Left,
        Right
    }
}
