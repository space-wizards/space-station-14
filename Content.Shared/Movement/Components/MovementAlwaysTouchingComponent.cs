using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

/// <summary>
/// Is this entity always considered to be touching a wall?
/// i.e. when weightless they're floaty but still have free movement.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MovementAlwaysTouchingComponent : Component
{

}
