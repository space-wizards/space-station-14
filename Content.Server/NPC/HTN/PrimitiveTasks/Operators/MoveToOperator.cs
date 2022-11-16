using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC.Components;
using Content.Server.NPC.Pathfinding;
using Content.Server.NPC.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

/// <summary>
/// Moves an NPC to the specified target key. Hands the actual steering off to NPCSystem.Steering
/// </summary>
public sealed class MoveToOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    private NPCSteeringSystem _steering = default!;
    private PathfindingSystem _pathfind = default!;

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
    public string TargetKey = "MovementTarget";

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

    private const string MovementCancelToken = "MovementCancelToken";

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _pathfind = sysManager.GetEntitySystem<PathfindingSystem>();
        _steering = sysManager.GetEntitySystem<NPCSteeringSystem>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        if (!blackboard.TryGetValue<EntityCoordinates>(TargetKey, out var targetCoordinates))
        {
            return (false, null);
        }

        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_entManager.TryGetComponent<TransformComponent>(owner, out var xform) ||
            !_entManager.TryGetComponent<PhysicsComponent>(owner, out var body))
            return (false, null);

        if (!_mapManager.TryGetGrid(xform.GridUid, out var ownerGrid) ||
            !_mapManager.TryGetGrid(targetCoordinates.GetGridUid(_entManager), out var targetGrid))
        {
            return (false, null);
        }

        var range = blackboard.GetValueOrDefault<float>(RangeKey);

        if (xform.Coordinates.TryDistance(_entManager, targetCoordinates, out var distance) && distance <= range)
        {
            // In range
            return (true, new Dictionary<string, object>()
            {
                {NPCBlackboard.OwnerCoordinates, blackboard.GetValueOrDefault<EntityCoordinates>(NPCBlackboard.OwnerCoordinates)}
            });
        }

        if (!PathfindInPlanning)
        {
            return (true, new Dictionary<string, object>()
            {
                {NPCBlackboard.OwnerCoordinates, targetCoordinates}
            });
        }

        var path = await _pathfind.GetPath(
            blackboard.GetValue<EntityUid>(NPCBlackboard.Owner),
            xform.Coordinates,
                targetCoordinates,
            range,
            cancelToken,
            _pathfind.GetFlags(blackboard));

        if (path.Result != PathResult.Path)
        {
            return (false, null);
        }

        return (true, new Dictionary<string, object>()
        {
            {NPCBlackboard.OwnerCoordinates, targetCoordinates},
            {PathfindKey, path}
        });

    }

    // Given steering is complicated we'll hand it off to a dedicated system rather than this singleton operator.

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);

        // Need to remove the planning value for execution.
        blackboard.Remove<EntityCoordinates>(NPCBlackboard.OwnerCoordinates);
        var targetCoordinates = blackboard.GetValue<EntityCoordinates>(TargetKey);

        // Re-use the path we may have if applicable.
        var comp = _steering.Register(blackboard.GetValue<EntityUid>(NPCBlackboard.Owner), targetCoordinates);

        if (blackboard.TryGetValue<float>(RangeKey, out var range))
        {
            comp.Range = range;
        }

        if (blackboard.TryGetValue<PathResultEvent>(PathfindKey, out var result))
        {
            if (blackboard.TryGetValue<EntityCoordinates>(NPCBlackboard.OwnerCoordinates, out var coordinates))
            {
                var mapCoords = coordinates.ToMap(_entManager);
                _steering.PrunePath(mapCoords, targetCoordinates.ToMapPos(_entManager) - mapCoords.Position, result.Path);
            }

            comp.CurrentPath = result.Path;
        }
    }

    public override void Shutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        base.Shutdown(blackboard, status);

        // Cleanup the blackboard and remove steering.
        if (blackboard.TryGetValue<CancellationTokenSource>(MovementCancelToken, out var cancelToken))
        {
            cancelToken.Cancel();
            blackboard.Remove<CancellationTokenSource>(MovementCancelToken);
        }

        // OwnerCoordinates is only used in planning so dump it.
        blackboard.Remove<PathResultEvent>(PathfindKey);

        if (RemoveKeyOnFinish)
        {
            blackboard.Remove<EntityCoordinates>(TargetKey);
        }

        _steering.Unregister(blackboard.GetValue<EntityUid>(NPCBlackboard.Owner));
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_entManager.TryGetComponent<NPCSteeringComponent>(owner, out var steering))
            return HTNOperatorStatus.Failed;

        return steering.Status switch
        {
            SteeringStatus.InRange => HTNOperatorStatus.Finished,
            SteeringStatus.NoPath => HTNOperatorStatus.Failed,
            SteeringStatus.Moving => HTNOperatorStatus.Continuing,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
