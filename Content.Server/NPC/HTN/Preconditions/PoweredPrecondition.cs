using Content.Server.Power.EntitySystems;

namespace Content.Server.NPC.HTN.Preconditions;

public sealed partial class PoweredPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private PowerReceiverSystem _power = default!;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _power = sysManager.GetEntitySystem<PowerReceiverSystem>();
    }

    public override bool IsMet(NPCBlackboard blackboard)
    {
        if (!blackboard.TryGetValue<EntityUid>(NPCBlackboard.Owner, out var owner, _entManager))
            return false;
        return _power.IsPowered(owner);
    }
}
