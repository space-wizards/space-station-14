using Robust.Shared.GameStates;

namespace Content.Shared.Delivery;

/// <summary>
/// Component given to deliveries.
/// Allows the delivery to be broken.
/// If intact, applies a small multiplier, otherwise substracts it.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(DeliveryModifierSystem))]
public sealed partial class DeliveryFragileComponent : Component
{
    /// <summary>
    /// Multiplier to use when the delivery is intact.
    /// </summary>
    [DataField]
    public float IntactMultiplierOffset = 0.15f;

    /// <summary>
    /// Multiplier to use when the delivery is broken.
    /// </summary>
    [DataField]
    public float BrokenMultiplierOffset = -0.33f;

    /// <summary>
    /// Whether this priority has already been broken or not.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Broken;
}
