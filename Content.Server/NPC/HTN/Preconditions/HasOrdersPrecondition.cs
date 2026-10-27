namespace Content.Server.NPC.HTN.Preconditions;

public sealed partial class HasOrdersPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;

    [DataField("orders", required: true)] public Enum Orders = default!;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        return Equals(blackboard.GetValueOrDefault<Enum>(NPCBlackboard.CurrentOrders, _entManager), Orders);
    }
}
