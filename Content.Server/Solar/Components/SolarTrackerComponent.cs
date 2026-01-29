using Robust.Shared.Map;

namespace Content.Server.Solar.Components;

[RegisterComponent]
public sealed partial class SolarTrackerComponent : Component
{
    /// <summary>
    /// The last coordinates that it was active on.
    /// When destroyed, the coordinates are not accurate - However, they're needed to reset the panels.
    /// Hence we save it here.
    /// </summary>
    [ViewVariables]
    public EntityCoordinates? LastCoordinates;
}
