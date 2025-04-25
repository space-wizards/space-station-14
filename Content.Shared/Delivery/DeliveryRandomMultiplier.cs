using Robust.Shared.GameStates;

namespace Content.Shared.Delivery;

/// <summary>
/// Component given to deliveries.
/// Applies a random multiplier to the delivery on init.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DeliveryRandomMultiplierComponent : Component
{
    /// <summary>
    /// The highest the random multiplier can go.
    /// </summary>
    [DataField]
    public float MaxMultiplier = 0.2f;

    /// <summary>
    /// The lowest the random multiplier can go.
    /// </summary>
    [DataField]
    public float MinMultiplier = -0.2f;

    /// <summary>
    /// The current multiplier this component provides.
    /// Gets randomized between MinMultiplier and MaxMultiplier on MapInit.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float CurrentMultiplier;
}
