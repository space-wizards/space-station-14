using Robust.Shared.GameStates;

namespace Content.Shared.Conveyor;

/// <summary>
/// Indicates this entity is actively being conveyed.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveConveyedComponent : Component
{

}
