using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;
using Content.Server.RoundEnd;

namespace Content.Server.StationEvents.Events;

public sealed class LoneOpsSpawnRule : StationEventSystem<LoneOpsSpawnRuleComponent>
{
    [Dependency] private readonly MapLoaderSystem _map = default!;

    protected override void Started(EntityUid uid, LoneOpsSpawnRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        // Loneops can only spawn if there is no nukeops active
        if (GameTicker.IsGameRuleAdded<NukeopsRuleComponent>())
        {
            ForceEndSelf(uid, gameRule);
            return;
        }

        var shuttleMap = MapManager.CreateMap();
        var options = new MapLoadOptions
        {
            LoadMap = true,
        };

        _map.TryLoad(shuttleMap, component.LoneOpsShuttlePath, out _, options);

        var nukeopsEntity = GameTicker.AddGameRule(component.GameRuleProto);
        component.AdditionalRule = nukeopsEntity;
        var nukeopsComp = Comp<NukeopsRuleComponent>(nukeopsEntity);
        nukeopsComp.SpawnOutpost = false;
        nukeopsComp.RoundEndBehavior = RoundEndBehavior.Nothing;
        GameTicker.StartGameRule(nukeopsEntity);
    }

    protected override void Ended(EntityUid uid, LoneOpsSpawnRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        if (component.AdditionalRule != null)
            GameTicker.EndGameRule(component.AdditionalRule.Value);
    }
}
