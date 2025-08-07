using Robust.Shared.GameStates;

namespace Content.Shared.Delivery;

/// <summary>
/// Component given to deliveries.
/// Applies a random multiplier to the delivery on init.
/// Added additively to the total multiplier.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(DeliveryModifierSystem))]
public sealed partial class DeliveryRandomMultiplierComponent : Component
{
    /// <summary>
    /// The highest the random multiplier can go.
    /// </summary>
    [DataField]
    public float MaxMultiplierOffset = 0.2f;

    /// <summary>
    /// The lowest the random multiplier can go.
    /// </summary>
    [DataField]
    public float MinMultiplierOffset = -0.2f;

    /// <summary>
    /// The current multiplier this component provides.
    /// Gets randomized between MaxMultiplierOffset and MinMultiplierOffset on MapInit.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float CurrentMultiplierOffset;
}
