using Robust.Shared.GameStates;

namespace Content.Shared.Speech.Components;

/// <summary>
/// Allows any accents on the entity to be relayed to other entities via InventoryRelayedEvent.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RelayAccentsComponent : Component
{

}
