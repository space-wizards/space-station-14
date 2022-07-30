using System.Threading.Tasks;
using Content.Server.AI.Systems;

namespace Content.Server.AI.HTN.PrimitiveTasks.Operators.Melee;

/// <summary>
/// Selects a target for melee.
/// </summary>
public sealed class PickMeleeTargetOperator : HTNOperator
{
    private AiFactionTagSystem _tags = default!;

    [ViewVariables, DataField("key")] public string Key = "CombatTarget";

    public override void Initialize()
    {
        base.Initialize();
        _tags = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<AiFactionTagSystem>();
    }

    public override async Task<Dictionary<string, object>?> Plan(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var radius = blackboard.GetValueOrDefault<float>(NPCBlackboard.VisionRadius);
        var targets = new List<(EntityUid Entity, float Rating)>();

        // TODO: Need a perception system instead
        foreach (var target in _tags
                     .GetNearbyHostiles(owner, radius))
        {
            targets.Add((target, GetRating(blackboard, target)));
        }

        targets.Sort((x, y) => x.Rating.CompareTo(y.Rating));

        // TODO: Add priority to
        // existing target
        // distance

        if (targets.Count == 0)
        {
            return null;
        }

        return new Dictionary<string, object>()
        {
            {Key, targets[0].Entity}
        };
    }

    private float GetRating(NPCBlackboard blackboard, EntityUid uid)
    {
        var rating = 0f;

        if (blackboard.TryGetValue<EntityUid>(Key, out var existingTarget) && existingTarget == uid)
        {
            rating += 3f;
        }

        return rating;
    }
}
