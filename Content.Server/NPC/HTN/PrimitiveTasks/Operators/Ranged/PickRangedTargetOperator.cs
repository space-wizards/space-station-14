using JetBrains.Annotations;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Ranged;

/// <summary>
/// Selects a target for ranged combat.
/// </summary>
[UsedImplicitly]
public sealed class PickRangedTargetOperator : NPCCombatOperator
{
    protected override float GetRating(NPCBlackboard blackboard, EntityUid uid, EntityUid existingTarget, float distance, bool canMove, EntityQuery<TransformComponent> xformQuery)
    {
        // Yeah look I just came up with values that seemed okay but they will need a lot of tweaking.
        // Having a debug overlay just to project these would be very useful when finetuning in future.
        var rating = 0f;

        if (existingTarget == uid)
        {
            rating += 2f;
        }

        rating += 1f / distance * 4f;
        return rating;
    }
}
