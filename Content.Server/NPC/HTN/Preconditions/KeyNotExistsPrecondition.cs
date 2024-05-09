namespace Content.Server.NPC.HTN.Preconditions;

public sealed partial class KeyNotExistsPrecondition : HTNPrecondition
{
    [DataField(required: true)]
    public string Key = string.Empty;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        return !blackboard.ContainsKey(Key);
    }
}
