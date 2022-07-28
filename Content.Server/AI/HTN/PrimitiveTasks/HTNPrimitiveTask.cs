using Robust.Shared.Prototypes;

namespace Content.Server.AI.HTN.PrimitiveTasks;

[Prototype("htnPrimitiveTask")]
public sealed class HTNPrimitiveTask : HTNTask
{
    /// <summary>
    /// What needs to be true for this task to be able to run.
    /// </summary>
    [DataField("preconditions")] public List<HTNPrecondition> Preconditions = new();

    [DataField("operator", required:true)] public HTNOperator Operator = default!;

    /// <summary>
    /// Effects that get run whenever this primitive task shuts down for any reason.
    /// </summary>
    [DataField("onShutdown")] public List<HTNEffect> Effects = new();
}
