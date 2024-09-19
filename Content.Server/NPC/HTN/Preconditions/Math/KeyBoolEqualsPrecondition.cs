namespace Content.Server.NPC.HTN.Preconditions.Math;

/// <summary>
/// Checks if there is a bool value for the specified <see cref="KeyBoolEqualsPrecondition.Key"/>
/// in the <see cref="NPCBlackboard"/> and the specified value is equal to the <see cref="KeyBoolEqualsPrecondition.Value"/>.
/// </summary>
public sealed partial class KeyBoolEqualsPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    [DataField(required: true), ViewVariables]
    public string Key = string.Empty;

    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public bool Value;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        if (!blackboard.TryGetValue<bool>(Key, out var value, _entManager))
            return false;

        return Value == value;
    }
}
