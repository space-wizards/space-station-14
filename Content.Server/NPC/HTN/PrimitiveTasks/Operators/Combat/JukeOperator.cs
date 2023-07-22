using Content.Server.NPC.Components;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Combat;

public sealed class JukeOperator : HTNOperator, IHtnConditionalShutdown
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    [DataField("shutdownState")]
    public HTNPlanState ShutdownState { get; } = HTNPlanState.PlanFinished;

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);
        _entManager.EnsureComponent<NPCJukeComponent>(blackboard.GetValue<EntityUid>(NPCBlackboard.Owner));
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        return HTNOperatorStatus.Finished;
    }

    public void ConditionalShutdown(NPCBlackboard blackboard)
    {
        _entManager.RemoveComponent<NPCJukeComponent>(blackboard.GetValue<EntityUid>(NPCBlackboard.Owner));
    }
}

public enum JukeType : byte
{
    /// <summary>
    /// Will move directly away from target if applicable.
    /// </summary>
    Away,

    AdjacentTile
}
