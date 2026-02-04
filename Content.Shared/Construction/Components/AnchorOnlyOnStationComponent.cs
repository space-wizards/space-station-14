namespace Content.Shared.Construction.Components;

/// <summary>
/// If a entity has this component it can only be anchored to the station
/// </summary>
[RegisterComponent]
public sealed partial class AnchorOnlyOnStationComponent : Component
{
    [DataField]
    public LocId PopupMessageAnchorFail = "anchorable-fail-not-on-station";
}
