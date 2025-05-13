using Robust.Shared.GameStates;

namespace Content.Shared.SprayPainter.Components;

/// <summary>
/// The component is used to charge the spray painter.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SprayPainterAmmoSystem))]
public sealed partial class SprayPainterAmmoComponent : Component
{
    /// <summary>
    /// The value by which the charge in the spray painter will be recharged.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Charges = 15;
}
