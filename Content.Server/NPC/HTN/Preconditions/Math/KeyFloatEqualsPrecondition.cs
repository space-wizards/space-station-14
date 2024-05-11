namespace Content.Server.NPC.HTN.Preconditions.Math;

public sealed partial class KeyFloatEqualsPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    [DataField(required: true)]
    public string Key = string.Empty;

    [DataField(required: true)]
    public float Value;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        return blackboard.TryGetValue<float>(Key, out var value, _entManager) &&
               MathHelper.CloseTo(value, value);
    }
}
