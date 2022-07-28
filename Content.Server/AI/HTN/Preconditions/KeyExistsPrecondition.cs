namespace Content.Server.AI.HTN.Preconditions;

public sealed class KeyExistsPrecondition : HTNPrecondition
{
    [DataField("key", required: true)] public string Key = string.Empty;

    public override bool IsMet(Dictionary<string, object> blackboard)
    {
        return blackboard.ContainsKey(Key);
    }
}
