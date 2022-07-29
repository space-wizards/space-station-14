using System.Threading;
using System.Threading.Tasks;
using Content.Server.AI.Components;
using Content.Server.AI.Pathfinding;
using Content.Server.AI.Pathfinding.Pathfinders;
using Content.Server.AI.Steering;
using Robust.Shared.Map;

namespace Content.Server.AI.HTN.PrimitiveTasks;

/// <summary>
/// Moves an NPC to the specified target key. Hands the actual steering off to NPCSystem.Steering
/// </summary>
public sealed class MoveToOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private PathfindingSystem _pathfind = default!;

    /// <summary>
    /// Should we assume the MovementTarget is reachable during planning or should we pathfind to it?
    /// </summary>
    [ViewVariables, DataField("pathfindInPlanning")]
    public bool PathfindInPlanning = true;

    [ViewVariables, DataField("key")]
    public string TargetKey = "MovementTarget";

    private const string MovementCancelToken = "MovementCancelToken";

    public override void Initialize()
    {
        base.Initialize();
        _pathfind = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<PathfindingSystem>();
    }

    public override async Task<Dictionary<string, object>?> Plan(NPCBlackboard blackboard)
    {
        if (!PathfindInPlanning)
            return null;

        var movementToken = new CancellationTokenSource();
        blackboard.SetValue(MovementCancelToken, movementToken);

        var job = _pathfind.RequestPath(
            new PathfindingArgs(
                blackboard.GetValue<EntityUid>(NPCBlackboard.Owner),
                new List<string>(), 0, TileRef.Zero, TileRef.Zero), movementToken.Token);

        await job.AsTask;

        blackboard.Remove(MovementCancelToken);

        if (job.Result == null)
            return null;

        return new Dictionary<string, object>()
        {
            { TargetKey, job.Result }
        };
    }

    // Given steering is complicated we'll hand it off to a dedicated system rather than this singleton operator.

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);
        var comp = _entManager.EnsureComponent<NPCSteeringComponent>(blackboard.GetValue<EntityUid>(NPCBlackboard.Owner));

        comp.Request = new GridTargetSteeringRequest(blackboard.GetValue<EntityCoordinates>(TargetKey), 1f);
    }

    public override void Shutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        base.Shutdown(blackboard, status);

        if (blackboard.TryGetValue<CancellationTokenSource>(MovementCancelToken, out var cancelToken))
        {
            cancelToken.Cancel();
            blackboard.Remove(MovementCancelToken);
        }

        blackboard.Remove(TargetKey);
        _entManager.RemoveComponent<NPCSteeringComponent>(blackboard.GetValue<EntityUid>(NPCBlackboard.Owner));
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_entManager.TryGetComponent<NPCSteeringComponent>(owner, out var steering))
            return HTNOperatorStatus.Failed;

        return steering.Status switch
        {
            SteeringStatus.Arrived => HTNOperatorStatus.Finished,
            SteeringStatus.NoPath => HTNOperatorStatus.Failed,
            SteeringStatus.Moving => HTNOperatorStatus.Continuing,
            SteeringStatus.Pending => HTNOperatorStatus.Continuing,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
