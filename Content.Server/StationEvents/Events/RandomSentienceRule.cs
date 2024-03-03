using System.Linq;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Ghost.Roles.Components;
using Content.Server.StationEvents.Components;

namespace Content.Server.StationEvents.Events;

public sealed class RandomSentienceRule : StationEventSystem<RandomSentienceRuleComponent>
{
    protected override void Started(EntityUid uid, RandomSentienceRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        HashSet<EntityUid> stationsToNotify = new();

        var targetList = new List<Entity<SentienceTargetComponent>>();
        var query = EntityQueryEnumerator<SentienceTargetComponent>();
        while (query.MoveNext(out var targetUid, out var target))
        {
            targetList.Add((targetUid, target));
        }

        RobustRandom.Shuffle(targetList);

        var toMakeSentient = RobustRandom.Next(2, 5);
        var groups = new HashSet<string>();

        foreach (var target in targetList)
        {
            if (toMakeSentient-- == 0)
                break;

            RemComp<SentienceTargetComponent>(target);
            var ghostRole = EnsureComp<GhostRoleComponent>(target);
            EnsureComp<GhostTakeoverAvailableComponent>(target);
            ghostRole.RoleName = MetaData(target).EntityName;
            ghostRole.RoleDescription = Loc.GetString("station-event-random-sentience-role-description", ("name", ghostRole.RoleName));
            groups.Add(Loc.GetString(target.Comp.FlavorKind));
        }

        if (groups.Count == 0)
            return;

        var groupList = groups.ToList();
        var kind1 = groupList.Count > 0 ? groupList[0] : "???";
        var kind2 = groupList.Count > 1 ? groupList[1] : "???";
        var kind3 = groupList.Count > 2 ? groupList[2] : "???";

        foreach (var target in targetList)
        {
            var station = StationSystem.GetOwningStation(target);
            if(station == null)
                continue;
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
