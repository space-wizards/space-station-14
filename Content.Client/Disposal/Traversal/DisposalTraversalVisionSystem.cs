using Content.Client.SubFloor;
using Content.Shared.Disposal.Traversal;
using Content.Shared.Disposal.Unit;
using Content.Shared.SubFloor;
using Robust.Client.Graphics;
using Robust.Client.Player;

namespace Content.Client.Disposal.Traversal;

/// <summary>
/// Manages traversal pipe overlays and subfloor reveal for the local player.
/// </summary>
public sealed partial class DisposalTraversalVisionSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlayManager = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private DisposalTraversalReachabilitySystem _reachability = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    private DisposalTraversalPipeOverlay? _pipeOverlay;
    private HashSet<EntityUid>? _reachableTubes;
    public IReadOnlySet<EntityUid>? ReachableTubes => _reachableTubes;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SubFloorHideComponent, GetSubFloorRevealEvent>(OnGetSubFloorReveal);

        _pipeOverlay = new DisposalTraversalPipeOverlay();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var reachableTubes = GetLocalReachableTubes();
        var inTraversal = reachableTubes != null;

        if (_pipeOverlay != null && _overlayManager.HasOverlay<DisposalTraversalPipeOverlay>() != inTraversal)
        {
            if (inTraversal)
                _overlayManager.AddOverlay(_pipeOverlay);
            else
                _overlayManager.RemoveOverlay(_pipeOverlay);
        }

        if (SameReachableTubes(reachableTubes))
            return;

        var oldReachableTubes = _reachableTubes;
        _reachableTubes = reachableTubes;

        QueueSubFloorUpdates(oldReachableTubes);
        QueueSubFloorUpdates(_reachableTubes);
    }

    private void OnGetSubFloorReveal(Entity<SubFloorHideComponent> ent, ref GetSubFloorRevealEvent args)
    {
        args.Revealed |= _reachableTubes?.Contains(ent.Owner) == true;
    }

    private HashSet<EntityUid>? GetLocalReachableTubes()
    {
        var player = _player.LocalSession?.AttachedEntity;
        if (!TryComp<BeingDisposedComponent>(player, out var beingDisposed) ||
            !TryComp<DisposalTraversalHolderComponent>(beingDisposed.Holder, out var holder) ||
            holder.CurrentTube == null)
        {
            return null;
        }

        return _reachability.GetReachableTubes(beingDisposed.Holder, holder.CurrentTube.Value);
    }

    private void QueueSubFloorUpdates(HashSet<EntityUid>? tubes)
    {
        if (tubes == null)
            return;

        foreach (var tube in tubes)
        {
            if (TryComp<AppearanceComponent>(tube, out var appearance))
                _appearance.QueueUpdate(tube, appearance);
        }
    }

    private bool SameReachableTubes(HashSet<EntityUid>? tubes)
    {
        if (_reachableTubes == null || tubes == null)
            return _reachableTubes == null && tubes == null;

        return _reachableTubes.SetEquals(tubes);
    }
}
