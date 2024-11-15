using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

/// <summary>
/// On entities that apply contact speed modifiers to airborne entities, e.g. flying mobs.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AffectAirborneMovementComponent : Component
{
}
