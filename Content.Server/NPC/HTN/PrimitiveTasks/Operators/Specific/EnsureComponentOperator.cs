using Robust.Shared.Prototypes;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Specific;

public sealed partial class EnsureComponentOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    /// <summary>
    /// Target entity to inject.
    /// </summary>
    [DataField(required: true)]
    public string TargetKey = string.Empty;

    /// <summary>
    /// Components to be added
    /// </summary>
    [DataField]
    public ComponentRegistry Components = new();

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entMan))
            return HTNOperatorStatus.Failed;

        _entMan.AddComponents(target, Components);
        return HTNOperatorStatus.Finished;
    }
}
