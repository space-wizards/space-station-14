using Content.Server.Hands.Systems;
using Content.Shared.Hands.Components;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Interactions;

/// <summary>
/// Drops the active hand entity underneath us.
/// </summary>
public sealed partial class DropOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        if (!blackboard.TryGetValue(NPCBlackboard.ActiveHand, out Hand? activeHand, _entManager))
        {
            return HTNOperatorStatus.Finished;
        }

        var owner = blackboard.GetValueOrDefault<EntityUid>(NPCBlackboard.Owner, _entManager);
        // TODO: Need some sort of interaction cooldown probably.
        var handsSystem = _entManager.System<HandsSystem>();

        if (handsSystem.TryDrop(owner))
        {
            return HTNOperatorStatus.Finished;
        }

        return HTNOperatorStatus.Failed;
    }
}
