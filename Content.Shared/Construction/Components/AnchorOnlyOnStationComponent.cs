using Robust.Shared.GameStates;

namespace Content.Shared.Construction.Components;

/// <summary>
/// If a entity has this component it can only be anchored to the station
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AnchorOnlyOnStationComponent : Component
{
    /// <summary>
    /// Pop up message when you try to anchor the entity on any grid that isn't the station grid
    /// </summary>
    [DataField]
    public LocId PopupMessageAnchorFail = "anchorable-fail-not-on-station";

    /// <summary>
    /// If true, it will only be able to be anchored on the largest grid of a station.
    /// If false, it will only be able to anchored on any grid of a station.
    /// </summary>
    [DataField]
    public bool OnlyCountLargestGrid = true;
}
