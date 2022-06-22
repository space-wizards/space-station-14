using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

/// <summary>
/// Handles movement inputs for shuttles. Relays movement to grid if the entity itself is not a grid.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class ShuttleMoverComponent : MoverComponent
{

}
