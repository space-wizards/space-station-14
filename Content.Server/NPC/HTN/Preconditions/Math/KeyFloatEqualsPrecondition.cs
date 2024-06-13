namespace Content.Server.NPC.HTN.Preconditions.Math;

/// <summary>
/// Checks if there is a float value for the specified <see cref="KeyFloatEqualsPrecondition.Key"/>
/// in the <see cref="NPCBlackboard"/> and the specified value is equal to the <see cref="KeyFloatEqualsPrecondition.Value"/>.
/// </summary>
public sealed partial class KeyFloatEqualsPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    [DataField(required: true), ViewVariables]
    public string Key = string.Empty;

    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public float Value;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        return blackboard.TryGetValue<float>(Key, out var value, _entManager) &&
               MathHelper.CloseTo(value, value);
    }
}
