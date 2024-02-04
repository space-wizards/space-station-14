using Content.Server.Transporters.Systems;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

public sealed partial class TransporterClaimOperator : HTNOperator
{
    private readonly TransporterSystem _transporters = default!;

    public string TargetKey = "Target";

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var target = blackboard.GetValue<EntityUid>(TargetKey);

        _transporters.ClaimItem(owner, target);

        Logger.Debug($"Transporter {owner.ToString()} has claimed item {target.ToString()}!");

        return HTNOperatorStatus.Finished;
    }
}
