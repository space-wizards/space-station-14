using Content.Shared.Interaction;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

public sealed partial class RotateToTargetOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    private RotateToFaceSystem _rotate = default!;

    [DataField("targetKey")]
    public string TargetKey = "RotateTarget";

    [DataField("rotateSpeedKey")]
    public string RotationSpeedKey = NPCBlackboard.RotateSpeed;

    // Didn't use a key because it's likely the same between all NPCs
    [DataField("tolerance")]
    public Angle Tolerance = Angle.FromDegrees(1);

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _rotate = sysManager.GetEntitySystem<RotateToFaceSystem>();
    }

    public override void TaskShutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        base.TaskShutdown(blackboard, status);
        blackboard.Remove<Angle>(TargetKey);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        if (!blackboard.TryGetValue<Angle>(TargetKey, out var rotateTarget, _entityManager))
        {
            return HTNOperatorStatus.Failed;
        }

        if (!blackboard.TryGetValue<float>(RotationSpeedKey, out var rotateSpeed, _entityManager))
        {
            return HTNOperatorStatus.Failed;
        }

        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (_rotate.TryRotateTo(owner, rotateTarget, frameTime, Tolerance, rotateSpeed))
        {
            return HTNOperatorStatus.Finished;
        }

        return HTNOperatorStatus.Continuing;
    }
}
