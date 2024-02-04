using Content.Server.Transporters.Systems;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

public sealed partial class TransporterDropItemOperator : HTNOperator
{
    private TransporterSystem _transporters = default!;

    public string TargetKey = "Target";

    public override void Initialize(IEntitySystemManager systemManager)
    {
        base.Initialize(systemManager);
        _transporters = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<TransporterSystem>();
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var target = blackboard.GetValue<EntityUid>(TargetKey);

        if (!_transporters.TransporterAttemptDrop(owner, target))
            return HTNOperatorStatus.Failed;

        return HTNOperatorStatus.Finished;
    }
}
