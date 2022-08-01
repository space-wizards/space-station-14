using System.Threading.Tasks;
using Content.Server.AI.Systems;
using Robust.Shared.Map;

namespace Content.Server.AI.HTN.PrimitiveTasks.Operators.Melee;

/// <summary>
/// Selects a target for melee.
/// </summary>
public sealed class PickMeleeTargetOperator : HTNOperator
{
    private AiFactionTagSystem _tags = default!;

    [ViewVariables, DataField("key")] public string Key = "CombatTarget";

    /// <summary>
    /// The EntityCoordinates of the specified target.
    /// </summary>
    [ViewVariables, DataField("keyCoordinates")]
    public string KeyCoordinates = "CombatTargetCoordinates";

    public override void Initialize()
    {
        base.Initialize();
        _tags = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<AiFactionTagSystem>();
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
        var rating = 0f;

        if (existingTarget == uid)
        {
            rating += 3f;
        }

        return rating;
    }
}
