namespace Content.Shared._Impstation.CosmicCult.Components;

/// <summary>
/// a marker component used as an extra flag for an event to toggle the monument preview.
/// could probably have a better name but idrk.
/// </summary>
[RegisterComponent]
public sealed partial class MonumentPlacementMarkerComponent : Component
{
    /// <summary>
    /// the tier of the monument that the overlay added by the event with this comp should render
    /// </summary>
    [DataField]
    public int Tier = 1;
}
