using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC.Components;
using Content.Server.NPC.Pathfinding;
using Content.Server.NPC.Systems;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;

namespace Content.Server._Starlight.NPC.HTN.PrimitiveTasks.Operators;

/// <summary>
/// Moves an NPC to the specified target key. Hands the actual steering off to NPCSystem.Steering
/// </summary>
public sealed partial class MoveFromOperator : HTNOperator, IHtnConditionalShutdown
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private NPCSteeringSystem _steering = default!;
    private PathfindingSystem _pathfind = default!;
    private SharedTransformSystem _transform = default!;

    /// <summary>
    /// When to shut the task down.
    /// </summary>
    [DataField("shutdownState")]
    public HTNPlanState ShutdownState { get; private set; } = HTNPlanState.TaskFinished;

    /// <summary>
    /// Should we assume the MovementTarget is reachable during planning or should we pathfind to it?
    /// </summary>
    [DataField("pathfindInPlanning")]
    public bool PathfindInPlanning = true;

    /// <summary>
    /// When we're finished moving to the target should we remove its key?
    /// </summary>
    [DataField("removeKeyOnFinish")]
    public bool RemoveKeyOnFinish = true;

    /// <summary>
    /// Target Coordinates to move to. This gets removed after execution.
    /// </summary>
    [DataField("targetKey")]
    public string TargetKey = "TargetCoordinates";

    /// <summary>
    /// Where the pathfinding result will be stored (if applicable). This gets removed after execution.
    /// </summary>
    [DataField("pathfindKey")]
    public string PathfindKey = NPCBlackboard.PathfindKey;

    /// <summary>
    /// How close we need to get before considering movement finished.
    /// </summary>
    [DataField("rangeKey")]
    public string RangeKey = "MovementRange";

    /// <summary>
    /// Do we only need to move into line of sight.
    /// </summary>
    [DataField("stopOnLineOfSight")]
    public bool StopOnLineOfSight;

    private const string MovementCancelToken = "MovementCancelToken";

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _pathfind = sysManager.GetEntitySystem<PathfindingSystem>();
        _steering = sysManager.GetEntitySystem<NPCSteeringSystem>();
        _transform = sysManager.GetEntitySystem<SharedTransformSystem>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        if (!blackboard.TryGetValue<EntityCoordinates>(TargetKey, out var threatCoords, _entManager))
            return (false, null);

        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_entManager.TryGetComponent<TransformComponent>(owner, out var xform) ||
            !_entManager.HasComponent<PhysicsComponent>(owner)) // If we don't have physics, we can't move
            return (false, null);

        var ownerPos = xform.Coordinates;

        if (!ownerPos.TryDistance(_entManager, threatCoords, out var dist))
            return (false, null);

        var safeDistance = blackboard.GetValueOrDefault<float>(RangeKey, _entManager);
        if (dist >= safeDistance)
            return (true, null);

        if (!PathfindInPlanning)
            return (true, new Dictionary<string, object>()
            {
                {NPCBlackboard.OwnerCoordinates, threatCoords}
            });

        var dir = (ownerPos.Position - threatCoords.Position).Normalized();
        var fleePos = threatCoords.Offset(dir * safeDistance * 1.5f);

        var path = await _pathfind.GetPath(owner, ownerPos, fleePos, safeDistance, cancelToken, _pathfind.GetFlags(blackboard));

        if (path.Result != PathResult.Path)
            return (true, null);

        return (true, new Dictionary<string, object>()
        {
            {NPCBlackboard.OwnerCoordinates, fleePos},
            {PathfindKey, path}
        });

    }

    // Given steering is complicated we'll hand it off to a dedicated system rather than this singleton operator.

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);

        // Need to remove the planning value for execution.
        blackboard.Remove<EntityCoordinates>(NPCBlackboard.OwnerCoordinates);

        var threatCoords = blackboard.GetValue<EntityCoordinates>(TargetKey);
        var uid = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        var ownerPos = _transform.GetMoverCoordinates(uid);
        var safeDistance = blackboard.GetValueOrDefault<float>(RangeKey, _entManager);
        var dir = (ownerPos.Position - threatCoords.Position).Normalized();
        var fleePos = threatCoords.Offset(dir * safeDistance * 1.5f);

        // Re-use the path we may have if applicable.
        var comp = _steering.Register(uid, fleePos);
        comp.ArriveOnLineOfSight = StopOnLineOfSight;

        if (blackboard.TryGetValue<float>(RangeKey, out var range, _entManager))
            comp.Range = range;

        if (blackboard.TryGetValue<PathResultEvent>(PathfindKey, out var result, _entManager))
        {
            var mapCoords = _transform.ToMapCoordinates(ownerPos);
            var path = result.Path;
            _steering.PrunePath(uid, mapCoords, _transform.ToMapCoordinates(fleePos).Position - mapCoords.Position, path);

            comp.CurrentPath = new Queue<PathPoly>(path);
        }
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        if (!_entManager.TryGetComponent<NPCSteeringComponent>(owner, out var steering))
            return HTNOperatorStatus.Failed;

        // Just keep moving in the background and let the other tasks handle it.
        if (blackboard.TryGetValue<EntityCoordinates>(TargetKey, out var threatCoords, _entManager))
        {
            var xform = _entManager.GetComponent<TransformComponent>(owner);
            if (xform.Coordinates.TryDistance(_entManager, threatCoords, out var dist))
            {
                var safeDistance = blackboard.GetValueOrDefault<float>(RangeKey, _entManager);
                if (dist >= safeDistance)
                    return HTNOperatorStatus.Finished;
            }
        }

        return steering.Status switch
        {
            SteeringStatus.InRange => HTNOperatorStatus.Finished,
            SteeringStatus.NoPath => HTNOperatorStatus.Failed,
            SteeringStatus.Moving => HTNOperatorStatus.Continuing,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public void ConditionalShutdown(NPCBlackboard blackboard)
    {
        // Cleanup the blackboard and remove steering.
        if (blackboard.TryGetValue<CancellationTokenSource>(MovementCancelToken, out var cancelToken, _entManager))
        {
            cancelToken.Cancel();
            blackboard.Remove<CancellationTokenSource>(MovementCancelToken);
        }

        // OwnerCoordinates is only used in planning so dump it.
        blackboard.Remove<PathResultEvent>(PathfindKey);

        if (RemoveKeyOnFinish)
            blackboard.Remove<EntityCoordinates>(TargetKey);

        _steering.Unregister(blackboard.GetValue<EntityUid>(NPCBlackboard.Owner));
    }
}
