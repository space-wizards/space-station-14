using Robust.Shared.GameStates;

namespace Content.Shared.Conveyor;

/// <summary>
/// Indicates this entity is currently being conveyed.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ConveyedComponent : Component;
