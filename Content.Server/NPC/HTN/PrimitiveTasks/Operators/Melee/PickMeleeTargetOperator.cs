using Robust.Shared.Map;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Melee;

/// <summary>
/// Selects a target for melee.
/// </summary>
public sealed class PickMeleeTargetOperator : NPCCombatOperator
{
    protected override float GetRating(NPCBlackboard blackboard, EntityUid uid, EntityUid existingTarget, bool canMove, EntityQuery<TransformComponent> xformQuery)
    {
        var ourCoordinates = blackboard.GetValueOrDefault<EntityCoordinates>(NPCBlackboard.OwnerCoordinates);

        if (!xformQuery.TryGetComponent(uid, out var targetXform))
            return -1f;

        var targetCoordinates = targetXform.Coordinates;

        if (!ourCoordinates.TryDistance(EntManager, targetCoordinates, out var distance))
            return -1f;

        var rating = 0f;

        if (existingTarget == uid)
        {
            rating += 3f;
        }

        rating += 1f / distance * 4f;

        return rating;
    }
}
