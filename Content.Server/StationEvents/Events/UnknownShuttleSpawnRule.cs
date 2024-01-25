using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Map;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;
using Robust.Shared.Random;
using Content.Server.Shuttles.Systems;

namespace Content.Server.StationEvents.Events;

public sealed class UnknownShuttleSpawnRule : StationEventSystem<UnknownShuttleSpawnRuleComponent>
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly ShuttlePreloaderSystem _preloadShuttle = default!;

    protected override void Started(EntityUid uid, UnknownShuttleSpawnRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (component.ShuttleVariants == null || component.ShuttleVariants.Count == 0)
            return;

        var shuttleMap = _mapManager.CreateMap();
        _mapManager.AddUninitializedMap(shuttleMap);

        var loadedShuttle = _preloadShuttle.TryGetPreloadedShuttle(_random.Pick(component.ShuttleVariants), shuttleMap);
        if (loadedShuttle == null)
            return;

        _mapManager.DoMapInitialize(shuttleMap);

        if (component.GameRuleProto != null)
        {
            var addedGameRule = _gameTicker.AddGameRule(component.GameRuleProto);
            component.SpawnedGameRule = addedGameRule;
            _gameTicker.StartGameRule(addedGameRule);
        }
    }

    protected override void Ended(EntityUid uid, UnknownShuttleSpawnRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        if (component.SpawnedGameRule != null)
            GameTicker.EndGameRule(component.SpawnedGameRule.Value);
    }

}
