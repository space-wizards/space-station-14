using Content.Server.Silicons.Bots;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Specific;

public sealed class MedibotInjectOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private MedibotSystem _medibotSystem = default!;

    [DataField("injectKey")]
    public string InjectKey = string.Empty;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _medibotSystem = sysManager.GetEntitySystem<MedibotSystem>();
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var target = blackboard.GetValue<EntityUid>(InjectKey);

        if (!target.IsValid() || _entManager.Deleted(target))
            return HTNOperatorStatus.Failed;

        if (!_entManager.TryGetComponent<MedibotComponent>(owner, out var medibot))
            return HTNOperatorStatus.Failed;

        if (medibot.CancelToken != null)
            return HTNOperatorStatus.Continuing;

        if (medibot.InjectTarget == null)
        {
            if (_medibotSystem.NPCStartInject(owner, target, medibot))
                return HTNOperatorStatus.Continuing;
            else
                return HTNOperatorStatus.Failed;
        }

        medibot.InjectTarget = null;

        return HTNOperatorStatus.Finished;
    }
}
