using Content.Server.AI.HTN.PrimitiveTasks;

namespace Content.Server.AI.HTN;

/// <summary>
/// The current plan for a HTN NPC.
/// </summary>
public sealed class HTNPlan
{
    public List<Dictionary<string, object>?> AppliedStates;

    public List<int> BranchTraversalRecord;

    public List<HTNPrimitiveTask> Tasks;

    public int Index = 0;

    public HTNPrimitiveTask CurrentTask => Tasks[Index];

    public HTNOperator CurrentOperator => CurrentTask.Operator;

    public HTNPlan(List<HTNPrimitiveTask> tasks, List<int> branchTraversalRecord, List<Dictionary<string, object>?> appliedStates)
    {
        Tasks = tasks;
        BranchTraversalRecord = branchTraversalRecord;
        AppliedStates = appliedStates;
    }
}
