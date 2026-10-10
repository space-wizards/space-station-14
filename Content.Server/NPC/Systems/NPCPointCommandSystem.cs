using Content.Server.NPC.HTN;

namespace Content.Server.NPC.Systems;

/// <summary>
/// Gives a group of NPCs a pointed combat target.
/// </summary>
public sealed partial class NPCPointCommandSystem : EntitySystem
{
    [Dependency] private HTNSystem _htn = default!;

    /// <summary>
    /// Gives targets to passed.
    /// </summary>
    public void CommandFollowers(
        HashSet<EntityUid> followers,
        EntityUid target)
    {
        foreach (var follower in followers)
        {
            if (TerminatingOrDeleted(follower) || !TryComp<HTNComponent>(follower, out var htn))
                continue;

            htn.Blackboard.SetValue(NPCBlackboard.CurrentOrderedTarget, target);
            ResetPlan(htn);
        }
    }

    /// <summary>
    /// Allows NPC to immediately start a new plan.
    /// </summary>
    public void ResetPlan(HTNComponent htn)
    {
        htn.PlanningToken?.Cancel();
        htn.PlanningToken = null;
        htn.PlanningJob = null;
        if (htn.Plan != null)
        {
            _htn.ShutdownTask(htn.Plan.CurrentOperator, htn.Blackboard, HTNOperatorStatus.Failed);
            _htn.ShutdownPlan(htn);
        }
        _htn.Replan(htn);
    }

}
