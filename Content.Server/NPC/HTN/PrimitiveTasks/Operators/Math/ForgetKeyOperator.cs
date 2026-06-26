namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Math;

/// <summary>
/// Wipes a specified key from the blackboard.
/// </summary>
public sealed partial class ForgetKeyOperator : HTNOperator
{
    /// <summary>
    ///  The key to remove.
    /// </summary>
    [DataField]
    public string key;

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);
        blackboard.Remove<EntityUid>(key);
    }
}
