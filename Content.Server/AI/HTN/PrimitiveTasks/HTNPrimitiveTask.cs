using Robust.Shared.Prototypes;

namespace Content.Server.AI.HTN.PrimitiveTasks;

[Prototype("htnPrimitiveTask")]
public sealed class HTNPrimitiveTask : HTNTask
{
    [DataField("operator", required:true)] public HTNOperator Operator = default!;
}
