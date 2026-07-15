using System.Numerics;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping.Components;

namespace Content.Shared.NodeCrawl;

public partial class NodeCrawlerMovementSystem
{
    private void OnCanTraverse(Entity<AtmosPipeLayersComponent> ent, ref NodeCrawlCanTraverseEvent args)
    {
        if (!HasComp<GasPipeManifoldComponent>(ent) && args.Movement.Comp.CurrentLayer != (int) ent.Comp.CurrentPipeLayer)
            args.Cancelled = true;
    }

    private void OnArrived(Entity<AtmosPipeLayersComponent> ent, ref NodeCrawlerArrivedAtNodeEvent args)
    {
        if (HasComp<GasPipeManifoldComponent>(ent))
            return;

        args.Movement.Comp.CurrentLayer = (int) ent.Comp.CurrentPipeLayer;
        Dirty(args.Movement);
    }

    private void OnBeforeMove(Entity<GasPipeManifoldComponent> ent, ref NodeCrawlBeforeMoveEvent args)
    {
        if (args.Movement.Comp.MoveVector == Vector2.Zero)
            return;

        var localDir = (args.Movement.Comp.MoveVector.GetDir().ToAngle() - Transform(ent).LocalRotation).GetCardinalDir();
        if (!TryGetManifoldLayer(args.Movement.Comp.CurrentLayer, new[] { Direction.North }, localDir, out var newLayer))
            return;

        args.Handled = true;
        if (newLayer == args.Movement.Comp.CurrentLayer)
            return;

        if (_gameTiming.CurTime < args.Movement.Comp.LastLayerSwitch + args.Movement.Comp.LayerSwitchCooldown)
            return;

        args.Movement.Comp.LastLayerSwitch = _gameTiming.CurTime;
        args.Movement.Comp.CurrentLayer = newLayer;
        args.Movement.Comp.TargetNode = null;
        Dirty(args.Movement);
    }

    /// <summary>
    /// Given the current layer, the manifold's exits, and the player's local move direction,
    /// picks the layer the crawler should turn onto.
    /// </summary>
    private static bool TryGetManifoldLayer(int currentLayer, Direction[] exits, Direction localDir, out int layer)
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
