using Content.Server._Starlight.Jump;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared._Starlight.Actions.Jump;
using Content.Shared.Throwing;
using Robust.Shared.Map;

namespace Content.Server._Starlight.NPC.HTN.PrimitiveTasks.Operators;

/// <summary>
/// NPC attempts to jump.
/// </summary
public sealed partial class JumpOperator : HTNOperator, IHtnConditionalShutdown
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    private JumpSystem _jumpSystem = default!;

    /// <summary>
    /// When to shut the task down.
    /// </summary>
    [DataField("shutdownState")]
    public HTNPlanState ShutdownState { get; private set; } = HTNPlanState.TaskFinished;

    /// <summary>
    /// When we're finished jumping to the target should we remove its key?
    /// </summary>
    [DataField("removeKeyOnFinish")]
    public bool RemoveKeyOnFinish = true;

    /// <summary>
    /// Target Coordinates to jump to. This gets removed after execution.
    /// </summary>
    [DataField("targetKey")]
    public string TargetKey = "TargetCoordinates";

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _jumpSystem = sysManager.GetEntitySystem<JumpSystem>();
    }

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);

        if (!blackboard.TryGetValue<EntityCoordinates>(TargetKey, out var jumpCoords, _entManager))
            return;

        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_entManager.TryGetComponent<JumpComponent>(owner, out var jumpComp))
            return;

        _jumpSystem.TryJump(new Entity<JumpComponent?>(owner, jumpComp), jumpCoords);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        if (!_entManager.TryGetComponent<JumpComponent>(owner, out var jump))
            return HTNOperatorStatus.Finished; // If we can't jump, we finish this.

        if (_entManager.TryGetComponent<ThrownItemComponent>(owner, out var thrown) && !thrown.Landed)
            return HTNOperatorStatus.Continuing;

        return HTNOperatorStatus.Finished;
    }

    public void ConditionalShutdown(NPCBlackboard blackboard)
    {
        if (RemoveKeyOnFinish)
            blackboard.Remove<EntityCoordinates>(TargetKey);
    }
}