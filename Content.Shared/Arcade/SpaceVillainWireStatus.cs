using Robust.Shared.Serialization;

namespace Content.Shared.Arcade;

[Serializable, NetSerializable]
public enum SpaceVillainWireStatus
{
    Overflow,
    InvinciblePlayer,
    InvincibleVillain,
}
