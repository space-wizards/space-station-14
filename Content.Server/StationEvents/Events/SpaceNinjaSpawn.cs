using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.StationEvents.Components;
using Content.Shared.Ninja.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.StationEvents.Events;

/// <summary>
/// Event for spawning a Space Ninja mid-game.
/// </summary>
public sealed class SpaceNinjaSpawn : StationEventSystem
{
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override string Prototype => "SpaceNinjaSpawn";

    public override void Started()
    {
        base.Started();

        // TODO: spawn outside station with a direction
        var spawnLocations = EntityManager.EntityQuery<MapGridComponent, TransformComponent>().ToList();

        if (spawnLocations.Count == 0)
        {
            Sawmill.Error($"No locations for space ninja to spawn!");
            return;
        }

        var location = _random.Pick(spawnLocations).Item2.MapPosition;
        Sawmill.Info($"Spawning space ninja at {location}");
        Spawn("MobHumanSpaceNinja", location);

        // start traitor rule incase it isn't, for the sweet greentext
        var rule = _proto.Index<GameRulePrototype>("Traitor");
        _ticker.StartGameRule(rule);
    }

    public override void Added()
    {
        Sawmill.Info("sus among us");
    }
}
