using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Map;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;

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

        var nukeopsEntity = _gameTicker.AddGameRule(component.GameRuleProto);
        component.AdditionalRule = nukeopsEntity;

        if (!_map.TryLoad(shuttleMap, component.LoneOpsShuttlePath, out var grids, options))
        {
            Logger.ErrorS("nukies", $"Error loading grid {component.LoneOpsShuttlePath} for lone operative!");
            return;
        }

        component.ShuttleOriginMap = shuttleMap; // SS220 Lone-Nukie-Declare-War

        var nukeopsComp = EntityManager.GetComponent<NukeopsRuleComponent>(nukeopsEntity);
        nukeopsComp.SpawnOutpost = false;
        nukeopsComp.EndsRound = false;
        nukeopsComp.NukieShuttle = grids[0]; // SS220 Lone-Nukie-Declare-War
        nukeopsComp.WarTCAmountPerNukie = component.WarTCAmount; // SS220 Lone-Nukie-Declare-War
        nukeopsComp.WarNukieArriveDelay = component.WarArriveDelay;
        _gameTicker.StartGameRule(nukeopsEntity);
    }

    protected override void Ended(EntityUid uid, LoneOpsSpawnRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        if (component.AdditionalRule != null)
            GameTicker.EndGameRule(component.AdditionalRule.Value);
    }
}

