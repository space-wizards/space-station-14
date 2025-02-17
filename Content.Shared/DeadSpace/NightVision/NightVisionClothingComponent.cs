using Robust.Shared.GameStates;

namespace Content.Server.DeadSpace.NightVision;

[RegisterComponent, NetworkedComponent]
public sealed partial class NightVisionClothingComponent : Component
{
    /// <summary>
    /// Night vision color for <see cref="NightVisionComponent"/>
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public Color Color = Color.LimeGreen;
}
