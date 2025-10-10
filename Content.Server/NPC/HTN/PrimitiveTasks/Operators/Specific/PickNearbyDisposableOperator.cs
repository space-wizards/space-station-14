using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC.Pathfinding;
using Content.Shared.Interaction;
using Content.Shared.Disposal.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Server.Containers;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Specific;

public sealed partial class PickNearbyDisposableOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private EntityLookupSystem _lookup = default!;
    private PathfindingSystem _pathfinding = default!;
    private ContainerSystem _container = default!;


    [DataField(required: true)]
    public string RangeKey = default!;

    /// <summary>
    /// Target entity to flush
    /// </summary>
    [DataField(required: true)]
    public string TargetKey = string.Empty;

    /// <summary>
    /// Target disposal bin entity
    /// </summary>
    [DataField(required: true)]
    public string DisposalTargetKey = string.Empty;

    /// <summary>
    /// Target entitycoordinates to move to.
    /// </summary>
    [DataField(required: true)]
    public string TargetMoveKey = string.Empty;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _lookup = sysManager.GetEntitySystem<EntityLookupSystem>();
        _pathfinding = sysManager.GetEntitySystem<PathfindingSystem>();
        _container = sysManager.GetEntitySystem<ContainerSystem>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<float>(RangeKey, out var range, _entManager))
            return (false, null);

        var disposalUnitQuery = _entManager.GetEntityQuery<DisposalUnitComponent>();
        var mailingUnitQuery = _entManager.GetEntityQuery<MailingUnitComponent>();
        var mobState = _entManager.GetEntityQuery<MobStateComponent>();

        foreach (var entity in _lookup.GetEntitiesInRange(owner, range))
        {
            if (!disposalUnitQuery.HasComponent(entity))
                continue;
            // dont want the mail boxes
            if (mailingUnitQuery.HasComponent(entity))
                continue;

            //checking if there is anyone NEAR the bin we found
            foreach (var flushable in _lookup.GetEntitiesInRange(entity, 1))
            {
                if (flushable == owner)
                    continue;

                if (_container.IsEntityInContainer(flushable))
                    continue;

                if (mobState.TryGetComponent(flushable, out var state))
                {
                    if (state.CurrentState != MobState.Critical)
                        continue;

                    var pathRange = SharedInteractionSystem.InteractionRange;
                    var path = await _pathfinding.GetPath(owner, flushable, pathRange, cancelToken);

                    if (path.Result == PathResult.NoPath)
                        return (false, null);

                    return (true, new Dictionary<string, object>()
                    {
                        {TargetKey, flushable},
                        {DisposalTargetKey, entity},
                        {TargetMoveKey, _entManager.GetComponent<TransformComponent>(flushable).Coordinates},
                        {NPCBlackboard.PathfindKey, path},
                    });
                }
            }

        }

        return (false, null);
    }
}
