using Content.Server.Interaction;
using Content.Shared.CombatMode;
using Content.Shared.DoAfter;
using Content.Shared.Timing;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Interactions;

public sealed partial class InteractWithOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private SharedDoAfterSystem _doAfterSystem = default!;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _doAfterSystem = sysManager.GetEntitySystem<SharedDoAfterSystem>();
    }

    /// <summary>
    /// Key that contains the target entity.
    /// </summary>
    [DataField(required: true)]
    public string TargetKey = default!;

    /// <summary>
    /// Exit with failure if doafter wasn't raised
    /// </summary>
    [DataField]
    public bool ExpectDoAfter = false;

    public string CurrentDoAfter = "CurrentInteractWithDoAfter";


    // Ensure that CurrentDoAfter doesn't exist as we enter this operator,
    // the code currently relies on the result of a TryGetValue
    public override void Startup(NPCBlackboard blackboard)
    {
        blackboard.Remove<ushort>(CurrentDoAfter);

    }

    // Not really sure if we should clean it up, I guess some operator could use it
    public override void TaskShutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        blackboard.Remove<ushort>(CurrentDoAfter);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        // Handle ongoing doAfter, and store the doAfter.nextId so we can detect if we started one
        ushort nextId = 0;
        if (_entManager.TryGetComponent<DoAfterComponent>(owner, out var doAfter))
        {
            // if CurrentDoAfter contains something, we have an active doAfter
            if (blackboard.TryGetValue<ushort>(CurrentDoAfter, out var doAfterId, _entManager))
            {
                var status = _doAfterSystem.GetStatus(owner, doAfterId, null);
                return status switch
                {
                    DoAfterStatus.Running => HTNOperatorStatus.Continuing,
                    DoAfterStatus.Finished => HTNOperatorStatus.Finished,
                    _ => HTNOperatorStatus.Failed
                };
            }

            nextId = doAfter.NextId;
        }


        if (_entManager.TryGetComponent<UseDelayComponent>(owner, out var useDelay) && _entManager.System<UseDelaySystem>().IsDelayed((owner, useDelay)) ||
            !blackboard.TryGetValue<EntityUid>(TargetKey, out var moveTarget, _entManager) ||
            !_entManager.TryGetComponent<TransformComponent>(moveTarget, out var targetXform))
        {
            return HTNOperatorStatus.Continuing;
        }

        if (_entManager.TryGetComponent<CombatModeComponent>(owner, out var combatMode))
        {
            _entManager.System<SharedCombatModeSystem>().SetInCombatMode(owner, false, combatMode);
        }

        _entManager.System<InteractionSystem>().UserInteraction(owner, targetXform.Coordinates, moveTarget);

        // Detect doAfter, save it, and don't exit from this operator
        if (doAfter != null && nextId != doAfter.NextId)
        {
            blackboard.SetValue(CurrentDoAfter, nextId);
            return HTNOperatorStatus.Continuing;
        }

        // We shouldn't arrive here if we start a doafter, so fail if we expected a doafter
        if(ExpectDoAfter)
            return HTNOperatorStatus.Failed;

        return HTNOperatorStatus.Finished;
    }
}
