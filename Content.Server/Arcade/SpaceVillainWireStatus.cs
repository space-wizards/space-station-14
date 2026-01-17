using Robust.Shared.Serialization;

namespace Content.Server.Arcade;

[Serializable, NetSerializable]
public enum SpaceVillainWireStatus
{
    Overflow,
    InvinciblePlayer,
    InvincibleVillain,
}
