using Content.Shared.Throwing;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Checks if the owner is being thrown or not
/// </summary>
public sealed partial class ThrownPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    [ViewVariables(VVAccess.ReadWrite)] [DataField] public bool IsBeingThrown = true;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        return IsBeingThrown == _entMan.HasComponent<ThrownItemComponent>(owner);
    }
}
