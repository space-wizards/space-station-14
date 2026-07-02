using Content.Shared.Buckle;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Checks if the owner is buckled or not
/// </summary>
public sealed partial class BuckledPrecondition : HTNPrecondition
{
    [Dependency] private SharedBuckleSystem _buckle = default!;

    [ViewVariables(VVAccess.ReadWrite)] [DataField("isBuckled")]
    public bool IsBuckled = true;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        return IsBuckled && _buckle.IsBuckled(owner) ||
               !IsBuckled && !_buckle.IsBuckled(owner);
    }
}
