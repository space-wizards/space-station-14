namespace Content.Server.AI.HTN.PrimitiveTasks;

[DataDefinition]
public sealed class HTNPrimitiveTask : HTNTask
{
    /// <summary>
    /// Should we re-apply our blackboard state as a result of our operator during startup?
    /// This means you can re-use old data, e.g. re-using a pathfinder result, and avoid potentially expensive operations.
    /// </summary>
    [DataField("applyEffectsOnStartup")] public bool ApplyEffectsOnStartup = true;

    /// <summary>
    /// What needs to be true for this task to be able to run.
    /// </summary>
    [DataField("preconditions")] public List<HTNPrecondition> Preconditions = new();

    [DataField("operator", required:true)] public HTNOperator Operator = default!;
}
