using Content.Shared.Movement.Pulling.Systems;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Checks if the owner is being pulled or not.
/// </summary>
public sealed partial class PulledPrecondition : HTNPrecondition
{
    [Dependency] private PullingSystem _pulling = default!;

    [ViewVariables(VVAccess.ReadWrite)] [DataField("isPulled")]
    public bool IsPulled = true;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        return IsPulled && _pulling.IsPulled(owner) ||
               !IsPulled && !_pulling.IsPulled(owner);
    }
}
