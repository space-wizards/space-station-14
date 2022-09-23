using JetBrains.Annotations;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Ranged;

/// <summary>
/// Selects a target for ranged combat.
/// </summary>
[MeansImplicitUse]
public sealed class PickRangedTargetOperator : NPCCombatOperator
{
    protected override float GetRating(NPCBlackboard blackboard, EntityUid uid, EntityUid existingTarget, float distance, bool canMove, EntityQuery<TransformComponent> xformQuery)
    {
        // TODO: Uhh make this better with penetration or something.
        var inLOS = Interaction.InRangeUnobstructed(blackboard.GetValue<EntityUid>(NPCBlackboard.Owner),
            uid, distance + 0.1f);

        if (!canMove && !inLOS)
            return -1f;

        // Yeah look I just came up with values that seemed okay but they will need a lot of tweaking.
        // Having a debug overlay just to project these would be very useful when finetuning in future.
        var rating = 0f;

        if (inLOS)
            rating += 4f;

        if (existingTarget == uid)
        {
            rating += 2f;
        }

        rating += 1f / distance * 4f;
        return rating;
    }
}
