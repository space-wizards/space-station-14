using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.SpaceVillain;

public abstract partial class SharedSpaceVillainArcadeSystem : EntitySystem
{ }

[Serializable, NetSerializable]
public enum SpaceVillainIndicators
{
    /// <summary>
    /// Blinks when any invincible flag is set
    /// </summary>
    HealthManager,
    /// <summary>
    /// Blinks when Overflow flag is set
    /// </summary>
    HealthLimiter
}

[Serializable, NetSerializable]
public enum SpaceVillainPlayerAction
{
    Attack,
    Heal,
    Recharge,
    NewGame,
    RequestData
}
