using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Fluids.EntitySystems;
using Content.Server.NPC.Pathfinding;
using Content.Shared.Fluids.Components;
using Robust.Shared.Map;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Fluid;

/// <summary>
/// Picks a nearby evaporatable puddle.
/// </summary>
public sealed class PickPuddleOperator : HTNOperator
{
    // This is similar to PickAccessibleComponent however I have an idea on generic utility queries
    // that can also be re-used for melee that needs further fleshing out.

    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    private PathfindingSystem _pathfinding = default!;
    private EntityLookupSystem _lookup = default!;

    [DataField("rangeKey", required: true)]
    public string RangeKey = string.Empty;

    [DataField("target")] public string Target = "Target";

    [DataField("targetKey", required: true)]
    public string TargetKey = string.Empty;

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
    [Obsolete("Obsolete")]
    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var range = blackboard.GetValueOrDefault<float>(RangeKey, _entManager);
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<EntityCoordinates>(NPCBlackboard.OwnerCoordinates, out var coordinates, _entManager))
        {
            return (false, null);
        }

        var targets = new List<EntityUid>();
        var puddleSystem = _entManager.System<PuddleSystem>();
        var solSystem = _entManager.System<SolutionContainerSystem>();

        foreach (var comp in _lookup.GetComponentsInRange<PuddleComponent>(coordinates, range))
        {
            if (comp.Owner == owner ||
                !solSystem.TryGetSolution(comp.Owner, comp.SolutionName, out var puddleSolution) ||
                puddleSystem.CanFullyEvaporate(puddleSolution))
            {
                continue;
            }

            targets.Add((comp.Owner));
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
                { Target, target },
                { TargetKey, xform.Coordinates },
                { PathfindKey, path}
            });
        }

        return (false, null);
    }
}
