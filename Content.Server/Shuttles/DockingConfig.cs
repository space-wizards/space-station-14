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
    /// Target grid for docking.
    /// </summary>
    public EntityUid TargetGrid;

    /// <summary>
    /// This is used for debugging.
    /// </summary>
    public Box2 Area;

    public EntityCoordinates Coordinates;

    /// <summary>
    /// Local angle of the docking grid relative to the target grid.
    /// </summary>
    public Angle Angle;
}
