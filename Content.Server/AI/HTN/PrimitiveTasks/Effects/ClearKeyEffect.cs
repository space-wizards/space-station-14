namespace Content.Server.AI.HTN.PrimitiveTasks;

public sealed class ClearKeyEffect : HTNEffect
{
    [DataField("key", required: true)] public string Key = string.Empty;

    public override void Effect(Dictionary<string, object> blackboard)
    {
        blackboard.Remove(Key);
    }
}
