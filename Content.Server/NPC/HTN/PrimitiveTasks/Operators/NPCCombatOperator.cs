using System.Threading;
using System.Threading.Tasks;
using Content.Server.Interaction;
using Content.Server.NPC.Pathfinding;
using Content.Server.NPC.Systems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.MobState;
using Content.Shared.MobState.Components;
using Robust.Shared.Map;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

public abstract class NPCCombatOperator : HTNOperator
{
    [Dependency] protected readonly IEntityManager EntManager = default!;
    private FactionSystem _factions = default!;
    protected InteractionSystem Interaction = default!;
    private PathfindingSystem _pathfinding = default!;

    [DataField("key")] public string Key = "CombatTarget";

    /// <summary>
    /// The EntityCoordinates of the specified target.
    /// </summary>
    [DataField("keyCoordinates")]
    public string KeyCoordinates = "CombatTargetCoordinates";

    /// <summary>
    /// Regardless of pathfinding or LOS these are the max we'll check
    /// </summary>
    private const int MaxConsideredTargets = 10;
    private const int MaxTargetCount = 5;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        sysManager.GetEntitySystem<ExamineSystemShared>();
        _factions = sysManager.GetEntitySystem<FactionSystem>();
        Interaction = sysManager.GetEntitySystem<InteractionSystem>();
        _pathfinding = sysManager.GetEntitySystem<PathfindingSystem>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var targets = await GetTargets(blackboard);

        if (targets.Count == 0)
        {
            return (false, null);
        }

        // TODO: Need some level of rng in ratings (outside of continuing to attack the same target)
        var selectedTarget = targets[0].Entity;

        var effects = new Dictionary<string, object>()
        {
            {Key, selectedTarget},
            {KeyCoordinates, new EntityCoordinates(selectedTarget, Vector2.Zero)}
        };

        return (true, effects);
    }

    private async Task<List<(EntityUid Entity, float Rating, float Distance)>> GetTargets(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var radius = blackboard.GetValueOrDefault<float>(NPCBlackboard.VisionRadius, EntManager);
        var targets = new List<(EntityUid Entity, float Rating, float Distance)>();

        blackboard.TryGetValue<EntityUid>(Key, out var existingTarget);
        var xformQuery = EntManager.GetEntityQuery<TransformComponent>();
        var mobQuery = EntManager.GetEntityQuery<MobStateComponent>();
        var canMove = blackboard.GetValueOrDefault<bool>(NPCBlackboard.CanMove, EntManager);
        var cancelToken = new CancellationTokenSource();
        var count = 0;

        if (xformQuery.TryGetComponent(existingTarget, out var targetXform))
        {
            var distance = await _pathfinding.GetPathDistance(owner, targetXform.Coordinates,
                SharedInteractionSystem.InteractionRange, cancelToken.Token, _pathfinding.GetFlags(blackboard));

            if (distance != null)
            {
                targets.Add((existingTarget, GetRating(blackboard, existingTarget, existingTarget, distance.Value, canMove, xformQuery), distance.Value));
            }
        }

        // TODO: Need a perception system instead
        // TODO: This will be expensive so will be good to optimise and cut corners.
        foreach (var target in _factions
                     .GetNearbyHostiles(owner, radius))
        {
            if (mobQuery.TryGetComponent(target, out var mobState) &&
                mobState.CurrentState > DamageState.Alive ||
                !xformQuery.TryGetComponent(target, out targetXform))
            {
                continue;
            }

            count++;

            if (count >= MaxConsideredTargets)
                break;

            if (!ExamineSystemShared.InRangeUnOccluded(owner, target, radius, null))
            {
                continue;
            }

            var distance = await _pathfinding.GetPathDistance(owner, targetXform.Coordinates,
                SharedInteractionSystem.InteractionRange, cancelToken.Token, _pathfinding.GetFlags(blackboard));

            if (distance == null)
                continue;

            targets.Add((target, GetRating(blackboard, target, existingTarget, distance.Value, canMove, xformQuery), distance.Value));

            if (targets.Count >= MaxTargetCount)
                break;
        }

        targets.Sort((x, y) => y.Rating.CompareTo(x.Rating));
        return targets;
    }

    protected abstract float GetRating(NPCBlackboard blackboard, EntityUid uid, EntityUid existingTarget, float distance, bool canMove,
        EntityQuery<TransformComponent> xformQuery);
}
