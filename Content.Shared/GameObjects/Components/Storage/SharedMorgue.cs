#nullable enable
using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Morgue
{
    [Serializable, NetSerializable]
    public enum MorgueVisuals
    {
        Open,
        HasContents,
        HasMob,
        HasSoul,
    }

    [Serializable, NetSerializable]
    public enum CrematoriumVisuals
    {
        Burning,
    }

    [Serializable, NetSerializable]
    public enum BodyBagVisuals
    {
        Label,
    }
}
