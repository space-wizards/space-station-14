using Content.Shared.Damage.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Damage.Components;

[RegisterComponent, NetworkedComponent]

/// <summary>
/// An entity with this component attached will prevent bypass effects of picking up an item with the DamageOnPickup Component.
/// </summary>
public sealed partial class DamageOnPickupImmuneComponent : Component
{
}
