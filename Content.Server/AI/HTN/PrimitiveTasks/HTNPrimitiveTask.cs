namespace Content.Server.AI.HTN.PrimitiveTasks;

[DataDefinition]
public sealed class HTNPrimitiveTask : HTNTask
{
    /// <summary>
    /// What needs to be true for this task to be able to run.
    /// </summary>
    [DataField("preconditions")] public List<HTNPrecondition> Preconditions = new();

    [DataField("operator", required:true)] public HTNOperator Operator = default!;
}
