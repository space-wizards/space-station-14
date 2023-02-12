using System.Linq;
using Content.Server.GameTicking;
using Content.Server.StationEvents.Components;
using Content.Server.Ninja.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.Ninja.Systems;

public sealed partial class NinjaSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override string Prototype => "SpaceNinjaSpawn";

    public override void Started()
    {
        // TODO: spawn outside station
        var spawnLocations = EntityManager.EntityQuery<MapGridComponent, TransformComponent>().ToList();

        if (spawnLocations.Count == 0)
            return;

        var location = _random.Pick(spawnLocations);
        Spawn("MobHumanSpaceNinja", location.Item2.MapPosition);
    }

    public override void Ended() { }
}
