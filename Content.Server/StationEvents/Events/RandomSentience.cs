using System.Linq;
using Content.Server.Chat;
using Content.Server.Chat.Systems;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Mind.Commands;
using Content.Server.Station.Systems;
using Content.Server.StationEvents.Components;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed class RandomSentience : StationEventSystem
{
    public override string Prototype => "RandomSentience";

    public override void Started()
    {
        base.Started();
        HashSet<EntityUid> stationsToNotify = new();

        var mod = GetSeverityModifier();
        var targetList = EntityManager.EntityQuery<SentienceTargetComponent>().ToList();
        RobustRandom.Shuffle(targetList);

        var toMakeSentient = (int) (RobustRandom.Next(2, 5) * Math.Sqrt(mod));
        var groups = new HashSet<string>();

        foreach (var target in targetList)
        {
            if (toMakeSentient-- == 0)
                break;

            EntityManager.RemoveComponent<SentienceTargetComponent>(target.Owner);
            var comp = EntityManager.AddComponent<GhostTakeoverAvailableComponent>(target.Owner);
            comp.RoleName = EntityManager.GetComponent<MetaDataComponent>(target.Owner).EntityName;
            comp.RoleDescription = Loc.GetString("station-event-random-sentience-role-description", ("name", comp.RoleName));
            groups.Add(Loc.GetString(target.FlavorKind));
        }

        if (groups.Count == 0)
            return;

        var groupList = groups.ToList();
        var kind1 = groupList.Count > 0 ? groupList[0] : "???";
        var kind2 = groupList.Count > 1 ? groupList[1] : "???";
        var kind3 = groupList.Count > 2 ? groupList[2] : "???";

        var entSysMgr = IoCManager.Resolve<IEntitySystemManager>();
        var stationSystem = entSysMgr.GetEntitySystem<StationSystem>();
        var chatSystem = entSysMgr.GetEntitySystem<ChatSystem>();
        foreach (var target in targetList)
        {
            var station = stationSystem.GetOwningStation(target.Owner);
            if(station == null) continue;
            stationsToNotify.Add((EntityUid) station);
        }
        foreach (var station in stationsToNotify)
        {
            chatSystem.DispatchStationAnnouncement(
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
