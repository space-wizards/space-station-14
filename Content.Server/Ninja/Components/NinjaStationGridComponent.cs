namespace Content.Server.Ninja.Components;

/// <summary>
/// Used by space ninja to indicate what station grid to head towards.
/// </summary>
[RegisterComponent]
public sealed class NinjaStationGridComponent : Component
{
    /// <summary>
    /// The grid uid being targeted.
    /// </summary>
    public EntityUid Grid;
}
