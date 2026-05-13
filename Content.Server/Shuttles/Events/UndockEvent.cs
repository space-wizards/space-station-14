using Content.Server.Shuttles.Components;

namespace Content.Server.Shuttles.Events;

/// <summary>
/// Raised whenever 2 grids undock.
/// </summary>
public sealed partial class UndockEvent : EntityEventArgs
{
    public DockingComponent DockA = default!;
    public DockingComponent DockB = default!;

    public EntityUid GridAUid = default!;
    public EntityUid GridBUid = default!;
}
