using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping.Components;
using Content.Shared.Disposal.Tube;
using Content.Shared.Disposal.Traversal;
using Content.Shared.VentCrawl.Components;
using Robust.Shared.Timing;

namespace Content.Shared.VentCrawl;

/// <summary>
/// Adds gas-pipe-specific traversal behavior to the generic disposal traversal system.
/// </summary>
public sealed partial class VentCrawlableSystem : EntitySystem
{
    [Dependency] private IGameTiming _gameTiming = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AtmosPipeLayersComponent, CanDisposalTraverseEvent>(OnCanTraverse);
        SubscribeLocalEvent<AtmosPipeLayersComponent, DisposalTraversalArrivedEvent>(OnArrived);
        SubscribeLocalEvent<GasPipeManifoldComponent, BeforeDisposalTraversalMoveEvent>(OnBeforeMove);
    }

    private void OnCanTraverse(Entity<AtmosPipeLayersComponent> ent, ref CanDisposalTraverseEvent args)
    {
        if (HasComp<GasPipeManifoldComponent>(ent))
            return;

        if (args.Holder.Comp.CurrentLayer != (int) ent.Comp.CurrentPipeLayer)
            args.Cancelled = true;
    }

    private void OnArrived(Entity<AtmosPipeLayersComponent> ent, ref DisposalTraversalArrivedEvent args)
    {
        if (HasComp<GasPipeManifoldComponent>(ent))
            return;

        args.Holder.Comp.CurrentLayer = (int) ent.Comp.CurrentPipeLayer;
        Dirty(args.Holder);
    }

    private void OnBeforeMove(Entity<GasPipeManifoldComponent> ent, ref BeforeDisposalTraversalMoveEvent args)
    {
        if (!TryComp<DisposalTubeComponent>(ent, out var traversable)
            || !TryComp<VentCrawlHolderComponent>(args.Holder, out var ventHolder))
            return;

        if (!args.Holder.Comp.IsMoving || args.Holder.Comp.CurrentDirection == Direction.Invalid)
            return;

        var manifoldRotation = Transform(ent).LocalRotation;
        var localDir = (args.Holder.Comp.CurrentDirection.ToAngle() - manifoldRotation).GetCardinalDir();

        if (!TryGetManifoldLayer(args.Holder.Comp.CurrentLayer, traversable.Exits, localDir, out var newLayer))
            return;

        if (newLayer == args.Holder.Comp.CurrentLayer)
            return;

        args.Handled = true;

        if (_gameTiming.CurTime < ventHolder.ManifoldLastLayerSelection + ventHolder.ManifoldLayerSelectionCooldown)
            return;

        ventHolder.ManifoldLastLayerSelection = _gameTiming.CurTime;
        args.Holder.Comp.CurrentLayer = newLayer;
        args.Holder.Comp.NextTube = null;
        Dirty(args.Holder);
        Dirty(args.Holder.Owner, ventHolder);
    }

    /// <summary>
    /// Converts side-input on a manifold into the layer the holder should occupy.
    /// </summary>
    private static bool TryGetManifoldLayer(
        int currentLayer,
        Direction[] exits,
        Direction localDir,
        out int layer)
    {
        layer = currentLayer;

        var hasVerticalExit = Array.IndexOf(exits, Direction.North) >= 0 || Array.IndexOf(exits, Direction.South) >= 0;
        var hasHorizontalExit = Array.IndexOf(exits, Direction.East) >= 0 || Array.IndexOf(exits, Direction.West) >= 0;

        if (hasVerticalExit == hasHorizontalExit)
            return false;

        var selectedLayer = (hasVerticalExit, localDir) switch
        {
            (true, Direction.West) => currentLayer == 2 ? 0 : 1,
            (true, Direction.East) => currentLayer == 1 ? 0 : 2,
            (false, Direction.North) => currentLayer == 2 ? 0 : 1,
            (false, Direction.South) => currentLayer == 1 ? 0 : 2,
            _ => (int?) null
        };

        if (selectedLayer == null)
            return false;

        layer = selectedLayer.Value;
        return true;
    }
}
