using System.Linq;
using Content.Server.StationEvents.Components;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed class VentCritters : StationEvent
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public static List<string> SpawnedPrototypeChoices = new List<string>()
        {"MobGiantSpiderAngry", "MobMouse", "MobMouse1", "MobMouse2"};

    public override string Name => "VentCritters";

    public override string? StartAnnouncement =>
        Loc.GetString("station-event-vent-spiders-start-announcement", ("data", Loc.GetString(Loc.GetString($"random-sentience-event-data-{_random.Next(1, 6)}"))));

    public override string? StartAudio => "/Audio/Announcements/bloblarm.ogg";

    public override int EarliestStart => 15;

    public override int MinimumPlayers => 15;

    public override float Weight => WeightLow;

    public override int? MaxOccurrences => 2;

    protected override float StartAfter => 30f;

    protected override float EndAfter => 60;

    public override void Startup()
    {
        base.Startup();
        var spawnChoice = _random.Pick(SpawnedPrototypeChoices);
        var spawnLocations = _entityManager.EntityQuery<VentCritterSpawnLocationComponent>().ToList();
        _random.Shuffle(spawnLocations);

        var spawnAmount = _random.Next(4, 12); // A small colony of critters.
        foreach (var location in spawnLocations)
        {
            if (spawnAmount-- == 0)
                break;

            var coords = _entityManager.GetComponent<TransformComponent>(location.Owner);

            _entityManager.SpawnEntity(spawnChoice, coords.Coordinates);
        }
    }
}
