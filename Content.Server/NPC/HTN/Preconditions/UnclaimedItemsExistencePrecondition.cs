using Content.Server.Transporters.Systems;

namespace Content.Server.NPC.HTN.Preconditions;

public sealed partial class UnclaimedItemsExistencePrecondition : HTNPrecondition
{
    private TransporterSystem _transporters = default!;

    public string TargetKey = "Target";

    [DataField()]
    public bool DoesntExist;

    public override void Initialize(IEntitySystemManager systemManager)
    {
        base.Initialize(systemManager);
        _transporters = systemManager.GetEntitySystem<TransporterSystem>();
    }

    public override bool IsMet(NPCBlackboard blackboard)
    {
        return _transporters.UnclaimedItemsExist() ^ DoesntExist;
    }
}
