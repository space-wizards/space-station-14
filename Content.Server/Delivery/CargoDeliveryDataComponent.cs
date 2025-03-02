using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Delivery;

/// <summary>
/// Component given to deliveries. I will write this eventually.
/// </summary>
[RegisterComponent]
public sealed partial class CargoDeliveryDataComponent : Component
{
    /// <summary>
    /// The time at which the next delivery will spawn.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextDelivery;

    /// <summary>
    /// Cooldown between deliveries after one spawns.
    /// </summary>
    [DataField]
    public TimeSpan DeliveryCooldown = TimeSpan.FromMinutes(5);

    /// <summary>
    /// The ratio at which deliveries will spawn, based on the amount of people in the crew manifest.
    /// 1 delivery per X players.
    /// </summary>
    [DataField]
    public int PlayerToDeliveryRatio = 7;

    /// <summary>
    /// Should deliveries be randomly split spawners?
    /// If true, the amount of deliveries will be spawned randomly across all spawners.
    /// If false, an amount of mail based on PlayerToDeliveryRatio will be spawned on all spawners.
    /// </summary>
    [DataField]
    public bool DistributeRandomly = true;
}
