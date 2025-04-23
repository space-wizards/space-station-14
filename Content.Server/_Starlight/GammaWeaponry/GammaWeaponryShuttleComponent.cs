namespace Content.Server.Starlight.GammaWeaponry;

[RegisterComponent]
public sealed partial class GammaWeaponryShuttleComponent : Component
{
    [DataField("station")]
    public EntityUid Station;
    
    [DataField]
    public string DockTag = "DockGamma";
}