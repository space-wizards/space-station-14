using Content.Server.Interaction;

namespace Content.Server.AI.HTN.PrimitiveTasks.Operators.Melee;

/// <summary>
/// Attacks the specified key in melee combat.
/// </summary>
public sealed class MeleeOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly InteractionSystem _interaction = default!;

    /// <summary>
    /// Key that contains the target entity.
    /// </summary>
    [ViewVariables, DataField("key", required: true)]
    public string Key = default!;

    /// <summary>
    /// Key that contains our range. When the target leaves this range we end combat.
    /// </summary>
    [ViewVariables, DataField("rangeKey", required: true)]
    public string RangeKey = default!;

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        base.Update(blackboard, frameTime);
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var target = blackboard.GetValue<EntityUid>(Key);

        if (_entManager.Deleted(target) ||
            !_entManager.TryGetComponent<TransformComponent>(target, out var targetXform) ||
            !_entManager.TryGetComponent<TransformComponent>(owner, out var ownerXform))
        {
            return HTNOperatorStatus.Failed;
        }

        // Out of range, abort.
        if (!ownerXform.Coordinates.InRange(_entManager, targetXform.Coordinates, blackboard.GetValue<float>(RangeKey)))
        {
            return HTNOperatorStatus.Failed;
        }

        // TODO: Like movement add a component and pass it off to the system.

        // TODO:
        // Need to be able to specify: Accuracy on moving targets
        // Should we hit until crit
        // Should we hit until destroyed
        // Juking
        // If target range above threshold (e.g. 0.7f) then move back into range
        _interaction.DoAttack(owner, targetXform.Coordinates, false, target);
        return HTNOperatorStatus.Continuing;
    }
}
