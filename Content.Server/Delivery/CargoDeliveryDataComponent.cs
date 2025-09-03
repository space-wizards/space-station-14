using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Delivery;

/// <summary>
/// Component given to a station to indicate it can have deliveries spawn on it.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CargoDeliveryDataComponent : Component
{
    /// <summary>
    /// The time at which the next delivery will spawn.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextDelivery;

    /// <summary>
    /// Minimum cooldown after a delivery spawns.
    /// </summary>
    [DataField]
    public TimeSpan MinDeliveryCooldown = TimeSpan.FromMinutes(3);

    /// <summary>
    /// Maximum cooldown after a delivery spawns.
    /// </summary>
    [DataField]
    public TimeSpan MaxDeliveryCooldown = TimeSpan.FromMinutes(7);


    /// <summary>
    /// The ratio at which deliveries will spawn, based on the amount of people in the crew manifest.
    /// 1 delivery per X players.
    /// </summary>
    [DataField]
    public float PlayerToDeliveryRatio = 7f;

    /// <summary>
    /// The minimum amount of deliveries that will spawn.
    /// This is not per spawner unless DistributeRandomly is false.
    /// </summary>
    [DataField]
    public int MinimumDeliverySpawn = 1;

    /// <summary>
    /// Should deliveries be randomly split between spawners?
    /// If true, the amount of deliveries will be spawned randomly across all spawners.
    /// If false, an amount of mail based on PlayerToDeliveryRatio will be spawned on all spawners.
    /// </summary>
    [DataField]
    public bool DistributeRandomly = true;
}
