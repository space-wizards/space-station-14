using System.Threading.Tasks;
using Content.Server.AI.Pathfinding;
using Content.Server.AI.Pathfinding.Accessible;
using Robust.Shared.Random;

namespace Content.Server.AI.HTN.PrimitiveTasks.Operators;

/// <summary>
/// Chooses a nearby coordinate and puts it into the resulting key.
/// </summary>
public sealed class PickAccessibleOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    private AiReachableSystem _reachable = default!;

    [DataField("idleRangeKey")] public string IdleRangeKey = "IdleRange";

    [ViewVariables, DataField("targetKey")]
    public string TargetKey = "MovementTarget";

    public override void Initialize()
    {
        base.Initialize();
        _reachable = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<AiReachableSystem>();
    }

    /// <inheritdoc/>
    public override async Task<Dictionary<string, object>?> Plan(NPCBlackboard blackboard)
    {
        // Very inefficient (should weight each region by its node count) but better than the old system
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_entManager.TryGetComponent(_entManager.GetComponent<TransformComponent>(owner).GridUid, out IMapGridComponent? grid))
            return null;

        var reachableArgs = ReachableArgs.GetArgs(owner);
        reachableArgs.VisionRadius = blackboard.GetValueOrDefault<float>(IdleRangeKey);
        var entityRegion = _reachable.GetRegion(owner);
        var reachableRegions = _reachable.GetReachableRegions(reachableArgs, entityRegion);

        if (reachableRegions.Count == 0)
            return null;

        var reachableNodes = new List<PathfindingNode>();

        foreach (var region in reachableRegions)
        {
            foreach (var node in region.Nodes)
            {
                reachableNodes.Add(node);
            }
        }

        var targetNode = _random.Pick(reachableNodes);

        var target = grid.Grid.GridTileToLocal(targetNode.TileRef.GridIndices);
        return new Dictionary<string, object>()
        {
            { TargetKey, target },
        };
    }
}
