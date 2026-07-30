namespace Content.Server.Station.Components;

/// <summary>
/// Marks a station for event eligibility.
/// </summary>
[RegisterComponent]
public sealed partial class StationEventEligibleComponent : Component
{

}

/// <summary>
/// Marks a grid, allowing events to configure their behavior for specific grids of a station.
/// </summary>
[RegisterComponent]
public sealed partial class GridEventEligibleComponent : Component
{
    /// <summary>
    /// See <see cref="PowerGridCheckRule"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool PowerGridChecks = true;
}
