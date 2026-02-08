using Content.Server.Cuffs;

namespace Content.Server.NPC.HTN.Preconditions;

public sealed partial class HandcuffedPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var cuffable = _entManager.System<CuffableSystem>();
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        return cuffable.IsCuffed(owner);
    }

}
