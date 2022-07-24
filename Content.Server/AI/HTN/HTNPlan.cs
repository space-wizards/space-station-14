using Content.Server.AI.HTN.PrimitiveTasks;

namespace Content.Server.AI.HTN;

/// <summary>
/// The current plan for a HTN NPC.
/// </summary>
public sealed class HTNPlan
{
    public HTNPrimitiveTask[] Tasks;

    public int Index = 0;

    public HTNPrimitiveTask CurrentTask => Tasks[0];

    public HTNOperator CurrentOperator => CurrentTask.Operator;

    public HTNPlan(HTNPrimitiveTask[] tasks)
    {
        Tasks = tasks;
    }
}
