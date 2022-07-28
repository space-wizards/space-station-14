namespace Content.Server.AI.HTN.PrimitiveTasks;

public sealed class MoveToOperator : HTNOperator
{
    [ViewVariables, DataField("key")]
    public string TargetKey = "MovementTarget";
}
