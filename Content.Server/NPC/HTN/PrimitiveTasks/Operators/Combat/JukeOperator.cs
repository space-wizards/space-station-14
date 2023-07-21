using Content.Server.NPC.Components;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Combat;

public sealed class JukeOperator : HTNOperator, IHtnConditionalShutdown
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    [DataField("shutdownState")]
    public HTNPlanState ShutdownState { get; } = HTNPlanState.Running;

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        // TODO:
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
