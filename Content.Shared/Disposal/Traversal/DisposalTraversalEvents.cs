using System.Numerics;
using Content.Shared.Disposal.Tube;

namespace Content.Shared.Disposal.Traversal;

/// <summary>
/// Raised before generic traversal movement is processed, allowing network-specific
/// systems to consume input or update holder state.
/// </summary>
[ByRefEvent]
public record struct BeforeDisposalTraversalMoveEvent(Entity<DisposalTraversalHolderComponent> Holder)
{
    public bool Handled;
}

/// <summary>
/// Raised to let network-specific systems filter candidate next segments.
/// </summary>
[ByRefEvent]
public record struct CanDisposalTraverseEvent(
    Entity<DisposalTraversalHolderComponent> Holder,
    EntityUid From,
    Entity<DisposalTubeComponent> To,
    Direction Direction)
{
    public bool Cancelled;
}

/// <summary>
/// Raised to request a position offset for a traversal segment.
/// </summary>
[ByRefEvent]
public record struct GetDisposalTraversalOffsetEvent(Entity<DisposalTraversalHolderComponent> Holder)
{
    public Vector2 Offset;
}

/// <summary>
/// Raised after a traversal holder enters a new segment.
/// </summary>
[ByRefEvent]
public record struct DisposalTraversalArrivedEvent(Entity<DisposalTraversalHolderComponent> Holder);
