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
    [DataField, AutoNetworkedField]
    public string? Text;

    /// <summary>
    /// A localization string that populates <see cref="Text"/> if it is null at mapinit.
    /// Used so that mappers can still override Text while mapping.
    /// </summary>
    [DataField]
    public LocId? DefaultText;

    [DataField, AutoNetworkedField]
    public Color Color = Color.Orange;

    /// <summary>
    /// Only enabled beacons can be seen on a station map.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;
}
