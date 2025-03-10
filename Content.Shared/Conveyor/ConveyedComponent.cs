using Robust.Shared.GameStates;

namespace Content.Shared.Conveyor;

/// <summary>
/// Indicates this entity is currently contacting a conveyor and will subscribe to events as appropriate.
/// For entities actively being conveyed see <see cref="ActiveConveyedComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ConveyedComponent : Component;
