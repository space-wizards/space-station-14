using System.Threading.Tasks;
using Content.Server.Interaction;
using Content.Server.NPC.Systems;
using Robust.Shared.Map;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Ranged;

/// <summary>
/// Selects a target for ranged combat.
/// </summary>
public sealed class PickRangedTargetOperator : HTNOperator
{
    // Should probably have an abstract that this and melee inherit from?

    [Dependency] private readonly IEntityManager _entManager = default!;
    private AiFactionTagSystem _tags = default!;
    private InteractionSystem _interaction = default!;

    [ViewVariables, DataField("key")] public string Key = "CombatTarget";

    /// <summary>
    /// The EntityCoordinates of the specified target.
    /// </summary>
    [ViewVariables, DataField("keyCoordinates")]
    public string KeyCoordinates = "CombatTargetCoordinates";

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _tags = sysManager.GetEntitySystem<AiFactionTagSystem>();
        _interaction = sysManager.GetEntitySystem<InteractionSystem>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var radius = blackboard.GetValueOrDefault<float>(NPCBlackboard.VisionRadius);
        var targets = new List<(EntityUid Entity, float Rating)>();

        blackboard.TryGetValue<EntityUid>(Key, out var existingTarget);

        // TODO: Need a perception system instead
        foreach (var target in _tags
                     .GetNearbyHostiles(owner, radius))
        {
            targets.Add((target, GetRating(blackboard, target, existingTarget)));
        }

        targets.Sort((x, y) => x.Rating.CompareTo(y.Rating));

        // TODO: Add priority to
        // existing target
        // distance

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

    private float GetRating(NPCBlackboard blackboard, EntityUid uid, EntityUid existingTarget)
    {
        var ourCoordinates = blackboard.GetValue<EntityCoordinates>(NPCBlackboard.OwnerCoordinates);
        var targetCoordinates = blackboard.GetValue<EntityCoordinates>(KeyCoordinates);

        if (!ourCoordinates.TryDistance(_entManager, targetCoordinates, out var distance))
            return -1f;

        var canMove = blackboard.GetValue<bool>(NPCBlackboard.CanMove);
        var inLOS = _interaction.InRangeUnobstructed(blackboard.GetValue<EntityUid>(NPCBlackboard.Owner),
            targetCoordinates, distance);

        if (!canMove && !inLOS)
            return -1f;

        var rating = 1f;

        if (inLOS)
            rating += 2f;

        if (existingTarget == uid)
        {
            rating += 4f;
        }

        rating += 1f / distance * 4f;
        return rating;
    }
}
