namespace Content.Server.NPC.HTN.Preconditions;

public sealed class GunAmmoPrecondition : HTNPrecondition
{
    [DataField("minPercent")]
    public float MinPercent = 0f;

    [DataField("maxPercent")]
    public float MaxPercent = 1f;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        return true;
    }
}
