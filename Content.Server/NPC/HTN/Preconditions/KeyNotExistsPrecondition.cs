namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Checks if there is no value at the specified  <see cref="KeyNotExistsPrecondition.Key"/> in the <see cref="NPCBlackboard"/>.
/// Returns true if there is no value.
/// </summary>
public sealed partial class KeyNotExistsPrecondition : HTNPrecondition
{
    [DataField(required: true), ViewVariables]
    public string Key = string.Empty;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        return !blackboard.ContainsKey(Key);
    }
}
