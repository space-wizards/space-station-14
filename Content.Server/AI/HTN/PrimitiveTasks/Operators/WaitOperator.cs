namespace Content.Server.AI.HTN.PrimitiveTasks;

public sealed class WaitOperator : HTNOperator
{
    /// <summary>
    /// Blackboard key for the time we'll wait for.
    /// </summary>
    [DataField("key")] public string Key = string.Empty;
}
