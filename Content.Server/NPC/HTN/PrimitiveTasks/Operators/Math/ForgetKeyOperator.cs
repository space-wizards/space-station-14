namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Math;

public sealed partial class ForgetKeyOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    [DataField("key")]
    public string key;

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);
        blackboard.Remove<EntityUid>(key);
    }
}