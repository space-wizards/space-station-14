using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Morgue
{
    [Serializable, NetSerializable]
    public enum MorgueVisuals : byte
    {
        Open,
        HasContents,
        HasMob,
        HasSoul,
    }

    [Serializable, NetSerializable]
    public enum CrematoriumVisuals : byte
    {
        Burning,
    }

    [Serializable, NetSerializable]
    public enum BodyBagVisuals : byte
    {
        Label,
    }
}
