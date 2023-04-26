using Content.Server.StationEvents.Events;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(SlimesSpawnRule))]
public sealed class SlimesSpawnRuleComponent : Component
{
	[DataField("SpawnedPrototypeChoices")]
    public List<string> SpawnedPrototypeChoices = new()
        {"MobAdultSlimesBlueAngry", "MobAdultSlimesGreenAngry", "MobAdultSlimesYellowAngry"};
}
