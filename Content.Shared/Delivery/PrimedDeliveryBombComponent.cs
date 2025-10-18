using Robust.Shared.GameStates;

namespace Content.Shared.Delivery;

/// <summary>
/// Component given to deliveries.
/// Indicates this bomb delivery is primed.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(DeliveryModifierSystem))]
public sealed partial class PrimedDeliveryBombComponent : Component;
