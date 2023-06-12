using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server.StationEvents.Components;
using Robust.Shared.Utility;

namespace Content.Server.StationEvents.Events;

public sealed class RandomSentienceRule : StationEventSystem<RandomSentienceRuleComponent>
{
    [Dependency] private readonly StationSystem _stationSystem = default!;

    protected override void Started(EntityUid uid, RandomSentienceRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        HashSet<EntityUid> stationsToNotify = new();

        var targetStation = _stationSystem.GetStations().FirstOrNull();

        if (!TryComp(targetStation, out StationDataComponent? data))
        {
            Logger.Info("TargetStation not have StationDataComponent");
            return;
        }

        var mod = GetSeverityModifier();
        var targetList = EntityQuery<SentienceTargetComponent, TransformComponent>().ToList();


        var grids = data.Grids.ToHashSet();
        targetList.RemoveAll(
            backupSpawnLoc =>
                backupSpawnLoc.Item2.GridUid.HasValue && !grids.Contains(backupSpawnLoc.Item2.GridUid.Value));

        RobustRandom.Shuffle(targetList);

        var toMakeSentient = (int) (RobustRandom.Next(2, 5) * Math.Sqrt(mod));
        var groups = new HashSet<string>();

        foreach (var target in targetList)
        {
            if (toMakeSentient-- == 0)
                break;

            RemComp<SentienceTargetComponent>(target.Item1.Owner);
            var ghostRole = EnsureComp<GhostRoleComponent>(target.Item1.Owner);
            EnsureComp<GhostTakeoverAvailableComponent>(target.Item1.Owner);
            ghostRole.RoleName = MetaData(target.Item1.Owner).EntityName;
            ghostRole.RoleDescription = Loc.GetString("station-event-random-sentience-role-description", ("name", ghostRole.RoleName));
            groups.Add(Loc.GetString(target.Item1.FlavorKind));
        }

        if (groups.Count == 0)
            return;

        var groupList = groups.ToList();
        var kind1 = groupList.Count > 0 ? groupList[0] : "???";
        var kind2 = groupList.Count > 1 ? groupList[1] : "???";
        var kind3 = groupList.Count > 2 ? groupList[2] : "???";

        foreach (var target in targetList)
        {
            var station = StationSystem.GetOwningStation(target.Item1.Owner);
            if(station == null) continue;
            stationsToNotify.Add((EntityUid) station);
        }
        foreach (var station in stationsToNotify)
        {
            ChatSystem.DispatchStationAnnouncement(
                station,
                Loc.GetString("station-event-random-sentience-announcement",
                    ("kind1", kind1), ("kind2", kind2), ("kind3", kind3), ("amount", groupList.Count),
                    ("data", Loc.GetString($"random-sentience-event-data-{RobustRandom.Next(1, 6)}")),
                    ("strength", Loc.GetString($"random-sentience-event-strength-{RobustRandom.Next(1, 8)}"))),
                playDefaultSound: false,
                colorOverride: Color.Gold
            );
        }
    }
}
