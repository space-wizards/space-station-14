using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

/// <summary>
/// Can this entity still move while its BodyStatus is InAir?
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class MovementIgnoreInAirComponent : Component
{

}
