using Content.Server.StationEvents.Events;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(RevenantSpawnRule))]
public sealed class RevenantSpawnRuleComponent : Component
{
    [DataField("revenantPrototype")]
    public string RevenantPrototype = "MobRevenant";
}
