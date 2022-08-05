using System.Threading.Tasks;
using Content.Server.NPC.Pathfinding;
using Content.Server.NPC.Pathfinding.Accessible;
using Robust.Shared.Random;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

/// <summary>
/// Chooses a nearby coordinate and puts it into the resulting key.
/// </summary>
public sealed class PickAccessibleOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    private AiReachableSystem _reachable = default!;

    [DataField("rangeKey", required: true)]
    public string RangeKey = string.Empty;

    [ViewVariables, DataField("targetKey", required: true)]
    public string TargetKey = string.Empty;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _reachable = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<AiReachableSystem>();
    }

    /// <inheritdoc/>
    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard)
    {
        // Very inefficient (should weight each region by its node count) but better than the old system
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_entManager.TryGetComponent(_entManager.GetComponent<TransformComponent>(owner).GridUid, out IMapGridComponent? grid))
            return (false, null);

        var reachableArgs = ReachableArgs.GetArgs(owner, blackboard.GetValueOrDefault<float>(RangeKey));
        var entityRegion = _reachable.GetRegion(owner);
        var reachableRegions = _reachable.GetReachableRegions(reachableArgs, entityRegion);

        if (reachableRegions.Count == 0)
            return (false, null);

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
        return (true, new Dictionary<string, object>()
        {
            { TargetKey, target },
        });
    }
}
