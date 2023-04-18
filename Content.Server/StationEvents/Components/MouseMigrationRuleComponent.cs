namespace Content.Server.StationEvents.Components;

[RegisterComponent]
public sealed class MouseMigrationRuleComponent : Component
{
    [DataField("spawnedPrototypeChoices")]
    public List<string> SpawnedPrototypeChoices = new() //we double up for that ez fake probability
    {
        "MobMouse",
        "MobMouse1",
        "MobMouse2",
        "MobRatServant"
    };
}
