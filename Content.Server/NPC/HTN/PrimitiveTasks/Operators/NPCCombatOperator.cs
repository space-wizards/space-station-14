using System.Threading;
using System.Threading.Tasks;
using Content.Server.Interaction;
using Content.Server.NPC.Pathfinding;
using Content.Server.NPC.Systems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
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

    protected virtual bool IsRanged => false;

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
        var ownerCoordinates = blackboard.GetValueOrDefault<EntityCoordinates>(NPCBlackboard.OwnerCoordinates, EntManager);
        var radius = blackboard.GetValueOrDefault<float>(NPCBlackboard.VisionRadius, EntManager);
        var targets = new List<(EntityUid Entity, float Rating, float Distance)>();

        blackboard.TryGetValue<EntityUid>(Key, out var existingTarget, EntManager);
        var xformQuery = EntManager.GetEntityQuery<TransformComponent>();
        var mobQuery = EntManager.GetEntityQuery<MobStateComponent>();
        var canMove = blackboard.GetValueOrDefault<bool>(NPCBlackboard.CanMove, EntManager);
        var count = 0;
        var paths = new List<Task>();
        // TODO: Really this should be a part of perception so we don't have to constantly re-plan targets.

        // Special-case existing target.
        if (EntManager.EntityExists(existingTarget))
        {
            paths.Add(UpdateTarget(owner, existingTarget, existingTarget, ownerCoordinates, blackboard, radius, canMove, xformQuery, targets));
        }

        // TODO: Need a perception system instead
        // TODO: This will be expensive so will be good to optimise and cut corners.
        foreach (var target in _factions
                     .GetNearbyHostiles(owner, radius))
        {
            if (mobQuery.TryGetComponent(target, out var mobState) &&
                mobState.CurrentState > MobState.Alive ||
                target == existingTarget ||
                target == owner)
            {
                continue;
            }

            count++;

            if (count >= MaxConsideredTargets)
                break;

            paths.Add(UpdateTarget(owner, target, existingTarget, ownerCoordinates, blackboard, radius, canMove, xformQuery, targets));
        }

        await Task.WhenAll(paths);

        targets.Sort((x, y) => y.Rating.CompareTo(x.Rating));
        return targets;
    }

    private async Task UpdateTarget(
        EntityUid owner,
        EntityUid target,
        EntityUid existingTarget,
        EntityCoordinates ownerCoordinates,
        NPCBlackboard blackboard,
        float radius,
        bool canMove,
        EntityQuery<TransformComponent> xformQuery,
        List<(EntityUid Entity, float Rating, float Distance)> targets)
    {
        if (!xformQuery.TryGetComponent(target, out var targetXform))
            return;

        var inLos = false;

        // If it's not an existing target then check LOS.
        if (target != existingTarget)
        {
            inLos = ExamineSystemShared.InRangeUnOccluded(owner, target, radius, null);

            if (!inLos)
                return;
        }

        // Turret or the likes, check LOS only.
        if (IsRanged && !canMove)
        {
            inLos = inLos || ExamineSystemShared.InRangeUnOccluded(owner, target, radius, null);

            if (!inLos || !targetXform.Coordinates.TryDistance(EntManager, ownerCoordinates, out var distance))
                return;

            targets.Add((target, GetRating(blackboard, target, existingTarget, distance, canMove, xformQuery), distance));
            return;
        }

        var nDistance = await _pathfinding.GetPathDistance(owner, targetXform.Coordinates,
            SharedInteractionSystem.InteractionRange, default, _pathfinding.GetFlags(blackboard));

        if (nDistance == null)
            return;

        targets.Add((target, GetRating(blackboard, target, existingTarget, nDistance.Value, canMove, xformQuery), nDistance.Value));
    }

    protected abstract float GetRating(NPCBlackboard blackboard, EntityUid uid, EntityUid existingTarget, float distance, bool canMove,
        EntityQuery<TransformComponent> xformQuery);
}
