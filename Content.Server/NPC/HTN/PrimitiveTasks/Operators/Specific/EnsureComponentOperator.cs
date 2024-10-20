using Robust.Shared.Prototypes;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Specific;

public sealed partial class EnsureComponentOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    /// <summary>
    /// Target entity to inject.
    /// </summary>
    [DataField("targetKey", required: true)]
    public string TargetKey = string.Empty;

    /// <summary>
    /// Components to be added
    /// </summary>
    [DataField("components")]
    public ComponentRegistry Components = new();

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entMan))
            return HTNOperatorStatus.Failed;

        _entMan.AddComponents(target, Components);
        return HTNOperatorStatus.Finished;
    }
}
