namespace Content.Server.AI.HTN.PrimitiveTasks;

public sealed class MoveToOperator : HTNOperator
{
    [ViewVariables, DataField("targetKey")]
    public string TargetKey = "MovementTarget";
}
