namespace Content.Server.AI.HTN.PrimitiveTasks;

/// <summary>
/// Chooses a nearby coordinate and puts it into the resulting key.
/// </summary>
public sealed class PickAccessibleOperator : HTNOperator
{
    [DataField("idleRangeKey")] public string IdleRangeKey = "IdleRange";

    [ViewVariables, DataField("key")]
    public string Key = "MovementTarget";
}
