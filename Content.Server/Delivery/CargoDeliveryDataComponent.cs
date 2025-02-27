using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Delivery;

/// <summary>
/// Component given to deliveries. I will write this eventually.
/// </summary>
[RegisterComponent]
public sealed partial class CargoDeliveryDataComponent : Component
{
    /// <summary>
    /// Deliveries that have been spawned but have not been delivered.
    /// </summary>
    [DataField]
    public List<EntityUid>? ActiveDeliveries;

    /// <summary>
    /// The max amount of undelivered deliveries that can exist on the station.
    /// If this number is surpassed, further deliveries cannot spawn.
    /// </summary>
    [DataField]
    public int DeliveryLimit = 15;

    /// <summary>
    /// The time at which the next delivery will spawn.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextDelivery;

    /// <summary>
    /// Cooldown between deliveries after one spawns.
    /// </summary>
    [DataField]
    public TimeSpan DeliveryCooldown = TimeSpan.FromMinutes(3);

    /// <summary>
    /// The amount of different deliveries that will be spawned.
    /// </summary>
    [DataField]
    public int DeliveryCount = 5;

    /// <summary>
    /// Should deliveries be split across spawners?
    /// If true, each spawner will spawn DeliveryCount / SpawnerAmount deliveries with a minimum of 1.
    /// If false, DeliveryCount amount will spawn on each spawner.
    /// </summary>
    [DataField]
    public bool SplitAcrossSpawners = true;
}
