namespace Content.Server.Stunnable.Components;



[RegisterComponent]
internal sealed class WebStunComponent : Component
{

    [DataField("paralyzeTime"), ViewVariables(VVAccess.ReadWrite)]
    public float ParalyzeTime = 5f;


    [DataField("webTrap")]
    public string WebTrap = "BroodyTrap";


    [DataField("fixture")] public string FixtureID = "projectile";


    [DataField("webPoly")]
    public string WebPolymorph = "HumanToCocon";


    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid AfterPoly;
}

