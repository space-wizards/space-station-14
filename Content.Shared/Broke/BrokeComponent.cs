using Robust.Shared.GameStates;

namespace Content.Shared.Broke;

/// <summary>
/// The component required for the operation of VendingMachine.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BrokeComponent : Component
{
    /// <summary>
    /// It is used as a flag indicating that the object is broken.
    /// </summary>
    public bool IsBroken;
}
