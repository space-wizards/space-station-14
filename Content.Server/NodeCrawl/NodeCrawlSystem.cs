using System.Linq;
using System.Numerics;
using Content.Server.Body.Systems;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.NodeCrawl;
using Content.Shared.Atmos;
using Content.Shared.NodeContainer;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;

namespace Content.Server.NodeCrawl;

public sealed class NodeCrawlSystem : SharedNodeCrawlSystem
{
    [Dependency] private readonly IReflectionManager _reflection = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrawlableNodeComponent, NodeGroupsRebuilt>(OnNodeGroupsRebuilt);

        SubscribeLocalEvent<NodeCrawlerComponent, InhaleLocationEvent>(OnInhaleLocation);
        SubscribeLocalEvent<NodeCrawlerComponent, ExhaleLocationEvent>(OnExhaleLocation);
        SubscribeLocalEvent<NodeCrawlerComponent, AtmosExposedGetAirEvent>(OnGetAir);
    }

    private Entity<NodeContainerComponent>? GetNodeContainer(Entity<NodeCrawlerComponent> crawler)
    {
        if (!TryComp<NodeCrawlerMovementComponent>(crawler.Comp.Mover, out var mover) || mover.Node is not { } node)
            return null;

        if (!TryComp<NodeContainerComponent>(node, out var nodeContainer))
            return null;

        return (node, nodeContainer);
    }

    private GasMixture? GetAir(Entity<NodeCrawlerComponent> crawler)
    {
        if (GetNodeContainer(crawler) is not { } nodeContainer)
            return null;

        foreach (var containedNode in nodeContainer.Comp.Nodes.Values)
        {
            if (containedNode is not PipeNode pipe)
                continue;

            return pipe.Air;
        }

        return null;
    }

    private void OnInhaleLocation(Entity<NodeCrawlerComponent> ent, ref InhaleLocationEvent args)
    {
        if (GetAir(ent) is not { } air)
            return;

        args.Gas = air;
    }

    private void OnExhaleLocation(Entity<NodeCrawlerComponent> ent, ref ExhaleLocationEvent args)
    {
        if (GetAir(ent) is not { } air)
            return;

        args.Gas = air;
    }

    private void OnGetAir(Entity<NodeCrawlerComponent> ent, ref AtmosExposedGetAirEvent args)
    {
        if (args.Handled || GetAir(ent) is not { } air)
            return;

        args.Gas = air;
        args.Handled = true;
    }

    private void OnNodeGroupsRebuilt(Entity<CrawlableNodeComponent> ent, ref NodeGroupsRebuilt args)
    {
        if (!TryComp<NodeContainerComponent>(ent, out var nodeContainer))
            return;

        // ugly workaround for https://github.com/space-wizards/RobustToolbox/issues/6694 not letting List<Type>
        // get serialized properly
        var possibleTypes = ent.Comp.ReachableNodeTypes.Select(it => _reflection.GetType(it)).ToList();

        ent.Comp.DeadEnd = false;
        var set = new HashSet<EntityUid>();
        foreach (var node in nodeContainer.Nodes.Values)
        {
            foreach (var reachable in node.ReachableNodes)
            {
                if (possibleTypes.Count != 0 && !possibleTypes.TrueForAll(type => reachable.GetType() == type))
                {
                    continue;
                }

                DebugTools.Assert(HasComp<CrawlableNodeComponent>(reachable.Owner), $"Node {ToPrettyString(reachable.Owner)} reachable from {ToPrettyString(ent)} should be a crawlable node, but wasn't");

                set.Add(reachable.Owner);
            }

            if (node is PipeNode pipeNode &&
                node.ReachableNodes.Count != BitOperations.PopCount((uint)pipeNode.CurrentPipeDirection))
            {
                ent.Comp.DeadEnd = true;
            }
        }

        ent.Comp.ReachableNodes = set;
        Dirty(ent);
    }
}
