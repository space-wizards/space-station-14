using System.Linq;
using Content.Server.StationEvents.Components;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed class MouseMigration : StationEvent
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public static List<string> SpawnedPrototypeChoices = new List<string>() //we double up for that ez fake probability
        {"MobMouse", "MobMouse1", "MobMouse2", "MobRatServant"};

    public override string Name => "MouseMigration";

    public override string? StartAnnouncement =>
        Loc.GetString("station-event-mouse-migration-announcement");

    public override int EarliestStart => 30;

    public override int MinimumPlayers => 35; //this just ensures that it doesn't spawn on lowpop maps. 

    public override float Weight => WeightLow;

    public override int? MaxOccurrences => 1;

    protected override float StartAfter => 30f;

    protected override float EndAfter => 60;

    public override void Startup()
    {
        base.Startup();
        
        var spawnLocations = _entityManager.EntityQuery<VentCritterSpawnLocationComponent, TransformComponent>().ToList();
        _random.Shuffle(spawnLocations);

        var spawnAmount = _random.Next(7, 15); // A small colony of critters.

        for (int i = 0; i < spawnAmount && i < spawnLocations.Count - 1; i++)
        {
            var spawnChoice = _random.Pick(SpawnedPrototypeChoices);
            if (_random.Prob(0.01f) || i == 0) //small chance for multiple, but always at least 1
                spawnChoice = "MobRatKing";

            _entityManager.SpawnEntity(spawnChoice, spawnLocations[i].Item2.Coordinates);
        }
    }
}
