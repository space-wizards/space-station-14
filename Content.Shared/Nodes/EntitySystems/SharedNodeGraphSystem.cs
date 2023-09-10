using Content.Shared.Nodes.Components;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Content.Shared.Nodes.EntitySystems;

/// <summary>
/// </summary>
public abstract partial class SharedNodeGraphSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming GameTiming = default!;
    protected EntityQuery<NodeGraphComponent> GraphQuery = default!;
    protected EntityQuery<GraphNodeComponent> NodeQuery = default!;

    protected HashSet<EntityUid> QueuedEdgeUpdates = new();
    protected HashSet<EntityUid> QueuedMergeGraphs = new();
    protected HashSet<EntityUid> QueuedSplitGraphs = new();

    public override void Initialize()
    {
        base.Initialize();

        GraphQuery = GetEntityQuery<NodeGraphComponent>();
        NodeQuery = GetEntityQuery<GraphNodeComponent>();

        SubscribeLocalEvent<GraphNodeComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<GraphNodeComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<NodeGraphComponent, ComponentShutdown>(OnComponentShutdown);
    }

    public override void Update(float frameTime)
    {
        const int maxUpdateIters = 100;

        base.Update(frameTime);
        if (!GameTiming.IsFirstTimePredicted)
            return;

        var curTime = GameTiming.CurTime;

        var iters = 0;
        bool loop;
        do
        {
            if (++iters > maxUpdateIters)
            {
                Log.Error($"Failed to resolve node graphs in {maxUpdateIters} iterations.");
                break;
            }
            loop = false;

            if (QueuedEdgeUpdates.Count > 0)
            {
                while (QueuedEdgeUpdates.FirstOrNull() is { } nodeId)
                {
                    UpdateEdges(nodeId, NodeQuery.GetComponent(nodeId));
                }
            }

            if (QueuedSplitGraphs.Count > 0)
            {
                loop = true;

                while (QueuedSplitGraphs.FirstOrNull() is { } graphId)
                {
                    ResolveSplits(graphId, curTime, GraphQuery.GetComponent(graphId));
                }
            }

            if (QueuedMergeGraphs.Count > 0)
            {
                loop = true;

                while (QueuedMergeGraphs.FirstOrNull() is { } graphId)
                {
                    ResolveMerges(graphId, curTime, GraphQuery.GetComponent(graphId));
                }
            }
        } while (loop);
    }
}
