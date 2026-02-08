using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.Enums;

[Serializable, NetSerializable]
public enum SpaceVillainArcadeWireStatus
{
    Overflow,
    InvinciblePlayer,
    InvincibleVillain,
}
