using Content.Shared.Hands.EntitySystems;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Interactions;

public sealed partial class EquipOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private SharedHandsSystem _handsSystem = default!;

    [DataField("target")]
    public string Target = "Target";

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        if (!blackboard.TryGetValue<EntityUid>(Target, out var target, _entManager))
        {
            return HTNOperatorStatus.Failed;
        }

        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        // TODO: As elsewhere need some generic interaction cooldown system
        if (_handsSystem.TryPickup(owner, target))
        {
            return HTNOperatorStatus.Finished;
        }

        return HTNOperatorStatus.Failed;
    }
}
