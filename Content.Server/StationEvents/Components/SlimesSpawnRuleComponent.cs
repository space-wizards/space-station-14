using Content.Server.StationEvents.Events;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(SlimesSpawnRule))]
public sealed class SlimesSpawnRuleComponent : Component
{
	[DataField("spawnedPrototypeChoices")]
    public List<string> SpawnedPrototypeChoices = new()
    {

	};
}
