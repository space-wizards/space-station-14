using Robust.Shared.GameStates;

namespace Content.Shared.Conveyor;

/// <summary>
/// Indicates this entity is blocking conveyed entities.
/// If this entity moves or otherwise shuts down we will try and re-activate the conveyed entities.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ConveyorBlockerComponent : Component
{
    /// <summary>
    /// Is it pending deletion.
    /// </summary>
    [DataField]
    public bool Expired = false;
}
