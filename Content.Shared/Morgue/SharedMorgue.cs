using Robust.Shared.Serialization;

namespace Content.Shared.Morgue;

[Serializable, NetSerializable]
public enum MorgueVisuals
{
    HasMob,
    HasSoul,
}

[Serializable, NetSerializable]
public enum CrematoriumVisuals
{
    Burning,
}
