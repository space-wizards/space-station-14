using System.Threading.Tasks;
using Content.Server.NPC.Pathfinding;
using Content.Server.NPC.Pathfinding.Accessible;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

/// <summary>
/// Picks a nearby component that is accessible.
/// </summary>
public sealed class PickAccessibleComponentOperator : HTNOperator
{
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    private AiReachableSystem _reachable = default!;
    private EntityLookupSystem _lookup = default!;

    [DataField("rangeKey", required: true)]
    public string RangeKey = string.Empty;

    [ViewVariables, DataField("targetKey", required: true)]
    public string TargetKey = string.Empty;

    [ViewVariables, DataField("component", required: true)]
    public string Component = string.Empty;

    public override void Initialize()
    {
        base.Initialize();
        _reachable = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<AiReachableSystem>();
    }

    /// <inheritdoc/>
    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard)
    {
        // Check if the component exists
        if (!_factory.TryGetRegistration(Component, out var registration))
        {
            return (false, null);
        }

        var range = blackboard.GetValueOrDefault<float>(RangeKey);
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<EntityCoordinates>(NPCBlackboard.OwnerCoordinates, out var coordinates))
        {
            return (false, null);
        }

        var comp = registration.Type;

        // TODO: Need to get ones that are accessible.
        // TODO: Look at unreal HTN to see repeatable ones maybe?
        foreach (var entity in _lookup.GetEntitiesInRange(coordinates, range))
        {
            // if (entity == owner || !_entManager)
                // continue;
        }

        // TODO: Copy over GoToPuddleSystem here.

        // Very inefficient (should weight each region by its node count) but better than the old system


        if (!_entManager.TryGetComponent(_entManager.GetComponent<TransformComponent>(owner).GridUid,
                out IMapGridComponent? grid))
        {
            return (false, null);
        }

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
