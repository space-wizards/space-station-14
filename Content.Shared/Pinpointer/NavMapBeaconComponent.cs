using Robust.Shared.GameStates;

namespace Content.Shared.Pinpointer;

/// <summary>
/// Will show a marker on a NavMap.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedNavMapSystem))]
[AutoGenerateComponentState]
public sealed partial class NavMapBeaconComponent : Component
{
    /// <summary>
    /// Defaults to entity name if nothing found.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    [AutoNetworkedField]
    public string? Text;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    [AutoNetworkedField]
    public Color Color = Color.Orange;

    /// <summary>
    /// Only enabled beacons can be seen on a station map.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    [AutoNetworkedField]
    public bool Enabled = true;
}
