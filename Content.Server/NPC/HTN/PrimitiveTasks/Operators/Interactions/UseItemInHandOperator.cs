using Content.Shared.Hands.EntitySystems;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Interactions;

/// <summary>
/// Uses the item in the NPC's active hand.
/// </summary>
public sealed partial class UseItemInHandOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<string>(NPCBlackboard.ActiveHand, out var activeHand, _entManager))
            return HTNOperatorStatus.Failed;

        var handsSystem = _entManager.System<SharedHandsSystem>();
        var success = handsSystem.TryUseItemInHand(owner, handName: activeHand);

        return success ? HTNOperatorStatus.Finished : HTNOperatorStatus.Failed;
    }
}
