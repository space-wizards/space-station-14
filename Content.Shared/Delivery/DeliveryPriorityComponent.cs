using Robust.Shared.GameStates;

namespace Content.Shared.Delivery;

/// <summary>
/// Component given to deliveries.
/// Applies a duration before which the delivery must be delivered.
/// If successful, adds a small multiplier, otherwise removes a small multiplier.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(DeliveryModifierSystem))]
public sealed partial class DeliveryPriorityComponent : Component
{
    /// <summary>
    /// The highest the random multiplier can go.
    /// </summary>
    [DataField]
    public float InTimeMultiplierOffset = 0.2f;

    /// <summary>
    /// The lowest the random multiplier can go.
    /// </summary>
    [DataField]
    public float ExpiredMultiplierOffset = -0.1f;

    /// <summary>
    /// Whether this priority delivery has already ran out of time or not.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Expired;

    /// <summary>
    /// How much time you get from spawn until the delivery expires.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan DeliveryTime = TimeSpan.FromMinutes(5);

    /// <summary>
    /// The time by which this has to be delivered.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan DeliverUntilTime;
}
