using Content.Server.StationEvents.Events;

namespace Content.Server.StationEvents.Components;

/// <summary>
/// Tarantula spawning component
/// <summary>
[RegisterComponent, Access(typeof(SpiderSpawnRule))]
public sealed class SpiderSpawnRuleComponent : Component
{

}

/// <summary>
/// Slime spawning component
/// <summary>
[RegisterComponent, Access(typeof(SlimesSpawnRule))]
public sealed class SlimesSpawnRuleComponent : Component
{
    [DataField("spawnedPrototypeChoices")]
    public List<string> SpawnedPrototypeChoices = new()
    {

    };
}
