using Robust.Shared.GameStates;

namespace Content.Shared.Pinpointer;

/// <summary>
/// Will show a marker on a NavMap.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class NavMapBeaconComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("text"), AutoNetworkedField]
    public string Text = string.Empty;

    [ViewVariables(VVAccess.ReadWrite), DataField("color"), AutoNetworkedField]
    public Color Color = Color.Orange;
}
