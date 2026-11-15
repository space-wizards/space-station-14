using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;

namespace Content.Server.NPC.HTN.Preconditions;

public sealed partial class HandcuffedPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private SharedCuffableSystem _cuffable = default!;

    [DataField]
    public bool ReactOnlyWhenFullyCuffed = true;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_entManager.TryGetComponent<CuffableComponent>(owner, out var cuffComp))
            return false;

        var target = (owner, cuffComp);

        return _cuffable.IsCuffed(target, ReactOnlyWhenFullyCuffed);
    }

}
