using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC.Pathfinding;
using Robust.Shared.Map;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

/// <summary>
/// Picks a nearby component that is accessible.
/// </summary>
public sealed partial class PickAccessibleComponentOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private PathfindingSystem _pathfinding = default!;
    private EntityLookupSystem _lookup = default!;

    [DataField("rangeKey", required: true)]
    public string RangeKey = string.Empty;

    [DataField("targetKey", required: true)]
    public string TargetKey = string.Empty;

    [DataField("target")]
    public string TargetEntity = "Target";

    [DataField("component", required: true)]
    public string Component = string.Empty;

    /// <summary>
    /// Where the pathfinding result will be stored (if applicable). This gets removed after execution.
    /// </summary>
    [DataField("pathfindKey")]
    public string PathfindKey = NPCBlackboard.PathfindKey;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _lookup = sysManager.GetEntitySystem<EntityLookupSystem>();
        _pathfinding = sysManager.GetEntitySystem<PathfindingSystem>();
    }

    /// <inheritdoc/>
    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        // Check if the component exists
        if (!_entManager.ComponentFactory.TryGetRegistration(Component, out var registration))
        {
            return (false, null);
        }

        var range = blackboard.GetValueOrDefault<float>(RangeKey, _entManager);
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<EntityCoordinates>(NPCBlackboard.OwnerCoordinates, out var coordinates, _entManager))
        {
            return (false, null);
        }

        var compType = registration.Type;
        var query = _entManager.GetEntityQuery(compType);
        var targets = new List<EntityUid>();

        // TODO: Need to get ones that are accessible.
        // TODO: Look at unreal HTN to see repeatable ones maybe?
        // TODO: Need type
        foreach (var entity in _lookup.GetEntitiesInRange(coordinates, range))
        {
            if (entity == owner || !query.TryGetComponent(entity, out var comp))
                continue;

            targets.Add(entity);
        }

        if (targets.Count == 0)
        {
            return (false, null);
        }

        foreach (var target in targets)
        {
            var path = await _pathfinding.GetPath(
                owner,
                target,
                1f,
                cancelToken,
                flags: _pathfinding.GetFlags(blackboard));

            if (path.Result != PathResult.Path)
            {
                return (false, null);
            }

            var xform = _entManager.GetComponent<TransformComponent>(target);

            return (true, new Dictionary<string, object>()
            {
                { TargetEntity, target },
                { TargetKey, xform.Coordinates },
                { PathfindKey, path }
            });
        }

        return (false, null);
    }
}
