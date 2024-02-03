using Content.Shared.Pulling;
using PullingSystem = Content.Shared.Movement.Pulling.Systems.PullingSystem;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Checks if the owner is being pulled or not.
/// </summary>
public sealed partial class PulledPrecondition : HTNPrecondition
{
    private PullingSystem _pulling = default!;

    [ViewVariables(VVAccess.ReadWrite)] [DataField("isPulled")] public bool IsPulled = true;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _pulling = sysManager.GetEntitySystem<PullingSystem>();
    }

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        return IsPulled && _pulling.IsPulled(owner) ||
               !IsPulled && !_pulling.IsPulled(owner);
    }
}
