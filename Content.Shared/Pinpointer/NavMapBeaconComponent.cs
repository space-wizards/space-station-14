using Robust.Shared.GameStates;

namespace Content.Shared.Pinpointer;

/// <summary>
/// Will show a marker on a NavMap.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NavMapBeaconComponent : Component
{
    /// <summary>
    /// Defaults to entity name if nothing found.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("text"), AutoNetworkedField]
    public string? Text;

    [ViewVariables(VVAccess.ReadWrite), DataField("color"), AutoNetworkedField]
    public Color Color = Color.Orange;
}
