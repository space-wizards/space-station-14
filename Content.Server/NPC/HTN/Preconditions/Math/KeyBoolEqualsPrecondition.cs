namespace Content.Server.NPC.HTN.Preconditions.Math;

/// <summary>
/// Checks for the presence of data in the blackboard and makes a comparison with the specified boolean
/// </summary>
public sealed partial class KeyBoolEqualsPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    [DataField(required: true)]
    public string Key = string.Empty;

    [DataField(required: true)]
    public bool Value;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        if (!blackboard.TryGetValue<bool>(Key, out var value, _entManager))
            return false;

        return Value == value;
    }
}
