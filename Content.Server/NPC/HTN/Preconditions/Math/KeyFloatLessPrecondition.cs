namespace Content.Server.NPC.HTN.Preconditions.Math;

public sealed partial class KeyFloatLessPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    [DataField(required: true)]
    public string Key = string.Empty;

    [DataField(required: true)]
    public float Value;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        return blackboard.TryGetValue<float>(NPCBlackboard.Owner, out var value, _entManager) && value < Value;
    }
}
