using Content.Server.StationEvents.Events;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(VentCrittersRule))]
public sealed class VentCrittersRuleComponent : Component
{
    [DataField("spawnedPrototypeChoices")]
    public List<string> SpawnedPrototypeChoices = new()
    {
        "MobMouse",
        "MobMouse1",
        "MobMouse2"
    };
}
