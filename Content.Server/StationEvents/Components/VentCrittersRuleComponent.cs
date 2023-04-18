namespace Content.Server.StationEvents.Components;

[RegisterComponent]
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
