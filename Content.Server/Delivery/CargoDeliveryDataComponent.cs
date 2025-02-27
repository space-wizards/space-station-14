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
    public TimeSpan DeliveryCooldown = TimeSpan.FromSeconds(30); // TODO: Bring this back to 2 or 3 minutes after testing is done

    /// <summary>
    /// The amount of different deliveries that will be spawned.
    /// </summary>
    [DataField]
    public int DeliveryCount = 5;

    /// <summary>
    /// Should deliveries be randomly split spawners?
    /// If true, the amount of deliveries will be spawned randomly across all spawners.
    /// If false, DeliveryCount amount will spawn on each spawner.
    /// </summary>
    [DataField]
    public bool DistributeRandomly = true;
}
