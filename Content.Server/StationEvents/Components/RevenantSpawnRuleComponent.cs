namespace Content.Server.StationEvents.Components;

[RegisterComponent]
public sealed class RevenantSpawnRuleComponent : Component
{
    [DataField("revenantPrototype")]
    public string RevenantPrototype = "MobRevenant";
}
