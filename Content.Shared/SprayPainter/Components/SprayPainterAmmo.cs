using Robust.Shared.GameStates;

namespace Content.Shared.SprayPainter.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SprayPainterAmmoComponent : Component
{
    [DataField]
    public int Charges { get; set; }
}
