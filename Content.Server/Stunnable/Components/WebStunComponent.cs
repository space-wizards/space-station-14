namespace Content.Server.Stunnable.Components;



[RegisterComponent]
internal sealed class WebStunComponent : Component
{

    [DataField("paralyzeTime"), ViewVariables(VVAccess.ReadWrite)]
    public float ParalyzeTime = 5f;


    [DataField("webTrap")]
    public string WebTrap = "BroodTrap";


    [DataField("fixture")] public string FixtureID = "projectile";

}

