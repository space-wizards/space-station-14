using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared.MapText;

/// <summary>
/// This is used for displaying text in world space
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MapTextComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public string? Text;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public Color Color = Color.White;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public string FontId = "Default";

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public int FontSize = 12;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public Vector2 Offset = Vector2.Zero;
}
