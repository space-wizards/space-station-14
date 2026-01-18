using Robust.Shared.Serialization;

namespace Content.Shared.Arcade;

[Serializable, NetSerializable]
public enum SpaceVillainArcadeWireStatus
{
    Overflow,
    InvinciblePlayer,
    InvincibleVillain,
}
