using Robust.Shared.GameStates;

namespace Content.Shared.Delivery;

/// <summary>
/// Component given to deliveries.
/// This delivery will blow up on being unlocked/broken/expired/etc.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(DeliveryModifierSystem))]
public sealed partial class DeliveryBombComponent : Component
{
    /// <summary>
    /// Multiplier to choose when a crazy person actually opens it.
    /// Multiplicative, not additive.
    /// </summary>
    [DataField]
    public float SpesoMultiplier = 2f;
}
