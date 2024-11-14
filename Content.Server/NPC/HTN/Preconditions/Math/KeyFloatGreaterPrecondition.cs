namespace Content.Server.NPC.HTN.Preconditions.Math;

/// <summary>
/// Checks if there is a float value for the specified <see cref="KeyFloatGreaterPrecondition.Key"/>
/// in the <see cref="NPCBlackboard"/> and the specified value is greater then <see cref="KeyFloatGreaterPrecondition.Value"/>.
/// </summary>
public sealed partial class KeyFloatGreaterPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    [DataField(required: true), ViewVariables]
    public string Key = string.Empty;

    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public float Value;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        return blackboard.TryGetValue<float>(Key, out var value, _entManager) && value > Value;
    }
}
