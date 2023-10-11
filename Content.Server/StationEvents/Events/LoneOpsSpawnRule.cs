using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Map;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;
using Content.Server.RoundEnd;

namespace Content.Server.StationEvents.Events;

public sealed class LoneOpsSpawnRule : StationEventSystem<LoneOpsSpawnRuleComponent>
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly NukeopsRuleSystem _nukeopsRuleSystem = default!;

    protected override void Started(EntityUid uid, LoneOpsSpawnRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!_nukeopsRuleSystem.CheckLoneOpsSpawn())
        {
            ForceEndSelf(uid, gameRule);
            return;
        }

        var shuttleMap = _mapManager.CreateMap();
        var options = new MapLoadOptions
        {
            LoadMap = true,
        };

        _map.TryLoad(shuttleMap, component.LoneOpsShuttlePath, out _, options);

        var nukeopsEntity = _gameTicker.AddGameRule(component.GameRuleProto);
        component.AdditionalRule = nukeopsEntity;
        var nukeopsComp = EntityManager.GetComponent<NukeopsRuleComponent>(nukeopsEntity);
        nukeopsComp.SpawnOutpost = false;
        nukeopsComp.RoundEndBehavior = RoundEndBehavior.Nothing;
        _gameTicker.StartGameRule(nukeopsEntity);
    }

    protected override void Ended(EntityUid uid, LoneOpsSpawnRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        if (component.AdditionalRule != null)
            GameTicker.EndGameRule(component.AdditionalRule.Value);
    }
}

