using Robust.Shared.GameStates;

namespace Content.Shared.SprayPainter.Components;

/// <summary>
/// The component is used to charge the spray painter.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SprayPainterAmmoComponent : Component
{
    /// <summary>
    /// The value by which the charge in the spray painter will be recharged.
    /// </summary>
    [DataField]
    public int Charges;
}
