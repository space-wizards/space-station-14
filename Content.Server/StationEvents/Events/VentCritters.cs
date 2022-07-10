using System.Linq;
using Content.Server.StationEvents.Components;
using Content.Shared.Sound;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed class VentCritters : StationEventSystem
{
    [Dependency] private readonly IRobustRandom RobustRandom = default!;
    [Dependency] private readonly IEntityManager EntityManager = default!;

    public static List<string> SpawnedPrototypeChoices = new List<string>()
        {"MobGiantSpiderAngry", "MobMouse", "MobMouse1", "MobMouse2"};

    public override string Name => "VentCritters";

    public override string? StartAnnouncement =>
        Loc.GetString("station-event-vent-spiders-start-announcement", ("data", Loc.GetString(Loc.GetString($"random-sentience-event-data-{RobustRandom.Next(1, 6)}"))));

    public override SoundSpecifier? StartAudio => new SoundPathSpecifier("/Audio/Announcements/aliens.ogg");

    public override int EarliestStart => 15;

    public override int MinimumPlayers => 15;

    public override float Weight => WeightLow;

    public override int? MaxOccurrences => 2;

    protected override float StartAfter => 30f;

    protected override float EndAfter => 60;

    public override bool AnnounceEvent => false;

    public override void Started()
    {
        base.Started();
        var spawnChoice = RobustRandom.Pick(SpawnedPrototypeChoices);
        var spawnLocations = EntityManager.EntityQuery<VentCritterSpawnLocationComponent>().ToList();
        RobustRandom.Shuffle(spawnLocations);

        var spawnAmount = RobustRandom.Next(4, 12); // A small colony of critters.
        foreach (var location in spawnLocations)
        {
            if (spawnAmount-- == 0)
                break;

            var coords = EntityManager.GetComponent<TransformComponent>(location.Owner);

            EntityManager.SpawnEntity(spawnChoice, coords.Coordinates);
        }
    }
}
