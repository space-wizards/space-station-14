using System.Linq;
using Content.Server.Chat;
using Content.Server.Chat.Systems;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Mind.Commands;
using Content.Server.Station.Systems;
using Content.Server.StationEvents.Components;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed class RandomSentience : StationEvent
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override string Name => "RandomSentience";

    public override float Weight => WeightNormal;

    protected override float EndAfter => 1.0f;

    public override void Startup()
    {
        base.Startup();
        HashSet<EntityUid> stationsToNotify = new();

        var targetList = _entityManager.EntityQuery<SentienceTargetComponent>().ToList();
        _random.Shuffle(targetList);

        var toMakeSentient = _random.Next(2, 5);
        var groups = new HashSet<string>();

        foreach (var target in targetList)
        {
            if (toMakeSentient-- == 0)
                break;

            MakeSentientCommand.MakeSentient(target.Owner, _entityManager);
            _entityManager.RemoveComponent<SentienceTargetComponent>(target.Owner);
            var comp = _entityManager.AddComponent<GhostTakeoverAvailableComponent>(target.Owner);
            comp.RoleName = _entityManager.GetComponent<MetaDataComponent>(target.Owner).EntityName;
            comp.RoleDescription = Loc.GetString("station-event-random-sentience-role-description", ("name", comp.RoleName));
            groups.Add(target.FlavorKind);
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
                (EntityUid) station,
                Loc.GetString("station-event-random-sentience-announcement",
                    ("kind1", kind1), ("kind2", kind2), ("kind3", kind3), ("amount", groupList.Count),
                    ("data", Loc.GetString($"random-sentience-event-data-{_random.Next(1, 6)}")),
                    ("strength", Loc.GetString($"random-sentience-event-strength-{_random.Next(1, 8)}"))),
                playDefaultSound: false,
                colorOverride: Color.Gold
            );
        }
    }
}
