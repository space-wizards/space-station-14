using Robust.Shared.GameStates;

namespace Content.Shared.Kitchen.Components;

/// <summary>
///
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedKitchenSpikeSystem))]
public sealed partial class KitchenSpikeUnhookingComponent : Component;
