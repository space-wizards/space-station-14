using Content.Server.Shuttles.Components;
using Robust.Shared.Map;

namespace Content.Server.Shuttles;

/// <summary>
/// Stores the data for a valid docking configuration for the emergency shuttle
/// </summary>
public sealed class DockingConfig
{
    /// <summary>
    /// The pairs of docks that can connect.
    /// </summary>
    public List<(EntityUid DockAUid, EntityUid DockBUid, DockingComponent DockA, DockingComponent DockB)> Docks = new();

    /// <summary>
    /// Area relative to the target grid the emergency shuttle will spawn in on.
    /// </summary>
    public Box2 Area;

    /// <summary>
    /// Target grid for docking.
    /// </summary>
    public EntityUid TargetGrid;

    public EntityCoordinates Coordinates;
    public Angle Angle;
}
