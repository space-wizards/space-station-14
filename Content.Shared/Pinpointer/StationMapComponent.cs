using Robust.Shared.GameStates;

namespace Content.Shared.Pinpointer;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StationMapComponent : Component
{
    /// <summary>
    /// Whether or not to show the user's location on the map.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ShowLocation = true;

    /// <summary>
    /// If true, when this entity initializes it will target and remember the station grid of the map the entity is in.
    /// If there is no station, the entity will target a random station in the current session.
    /// </summary>
    [DataField]
    public bool InitializeWithStation = false;

    /// <summary>
    /// The target grid that the map will display.
    /// If null, it will display the user's current grid.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? TargetGrid;
}
