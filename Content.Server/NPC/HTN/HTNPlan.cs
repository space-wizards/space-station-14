using Content.Server.NPC.HTN.PrimitiveTasks;

namespace Content.Server.NPC.HTN;

/// <summary>
/// The current plan for a HTN NPC.
/// </summary>
public sealed class HTNPlan
{
    /// <summary>
    /// Effects that were applied for each primitive task in the plan.
    /// </summary>
    public readonly List<Dictionary<string, object>?> Effects;

    public readonly List<int> BranchTraversalRecord;

    public readonly List<HTNTask> Tasks;

    public int Index = 0;

    public HTNTask CurrentTask => Tasks[Index];

    public HTNPlan(List<HTNTask> tasks, List<int> branchTraversalRecord, List<Dictionary<string, object>?> effects)
    {
        Tasks = tasks;
        BranchTraversalRecord = branchTraversalRecord;
        Effects = effects;
    }
}
