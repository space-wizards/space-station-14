using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC.Pathfinding;
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
    private PathfindingSystem _path = default!;
    private EntityLookupSystem _lookup = default!;

    [DataField("rangeKey", required: true)]
    public string RangeKey = string.Empty;

    [ViewVariables, DataField("targetKey", required: true)]
    public string TargetKey = string.Empty;

    [ViewVariables, DataField("component", required: true)]
    public string Component = string.Empty;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _path = sysManager.GetEntitySystem<PathfindingSystem>();
        _lookup = sysManager.GetEntitySystem<EntityLookupSystem>();
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

        var compType = registration.Type;
        var query = _entManager.GetEntityQuery(compType);
        var targets = new List<Component>();

        // TODO: Need to get ones that are accessible.
        // TODO: Look at unreal HTN to see repeatable ones maybe?
        foreach (var entity in _lookup.GetEntitiesInRange(coordinates, range))
        {
            if (entity == owner || !query.TryGetComponent(entity, out var comp))
                continue;

            targets.Add(comp);
        }

        if (targets.Count == 0)
        {
            return (false, null);
        }

        while (targets.Count > 0)
        {
            // TODO: Get nearest at some stage
            var target = _random.PickAndTake(targets);

            // TODO: God the path api sucks PLUS I need some fast way to get this.
            var job = _path.RequestPath(owner, target.Owner, CancellationToken.None);

            if (job == null)
                continue;

            await job.AsTask;

            if (job.Result == null || !_entManager.TryGetComponent<TransformComponent>(target.Owner, out var targetXform))
            {
                continue;
            }

            return (true, new Dictionary<string, object>()
            {
                { TargetKey, targetXform.Coordinates },
            });
        }

        return (false, null);
    }
}
