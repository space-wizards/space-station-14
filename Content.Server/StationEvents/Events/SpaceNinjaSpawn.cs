using System.Linq;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.StationEvents.Components;
using Content.Server.Ninja.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed class SpaceNinjaSpawn : StationEventSystem
{
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override string Prototype => "SpaceNinjaSpawn";

    public override void Started()
    {
        // TODO: spawn outside station with a direction
        var spawnLocations = EntityManager.EntityQuery<MapGridComponent, TransformComponent>().ToList();

        if (spawnLocations.Count == 0)
            return;

        var location = _random.Pick(spawnLocations);
        Spawn("MobHumanSpaceNinja", location.Item2.MapPosition);

        // start traitor rule incase it isn't, for the sweet greentext
        var rule = _proto.Index<GameRulePrototype>("Traitor");
        _ticker.StartGameRule(rule);
    }

    public override void Ended() { }
}
