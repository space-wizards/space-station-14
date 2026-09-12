using System.Linq;
using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Server.StationEvents.Components;
using Content.Shared.Dataset;
using Content.Shared.GameTicking.Components;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed partial class RandomSentienceRule : StationEventSystem<RandomSentienceRuleComponent>
{
    private static readonly ProtoId<LocalizedDatasetPrototype> DataSourceNames = "RandomSentienceEventData";
    private static readonly ProtoId<LocalizedDatasetPrototype> IntelligenceLevelNames = "RandomSentienceEventStrength";

    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private GhostRoleSystem _ghostRole = default!;

    protected override void Started(EntityUid uid, RandomSentienceRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        if (!TryGetRandomStation(out var station))
            return;

        var targetList = new List<Entity<SentienceTargetComponent>>();
        var query = EntityQueryEnumerator<SentienceTargetComponent, TransformComponent>();
        while (query.MoveNext(out var targetUid, out var target, out var xform))
        {
            if (StationSystem.GetOwningStation(targetUid, xform) != station)
                continue;

            targetList.Add((targetUid, target));
        }

        var toMakeSentient = _random.Next(component.MinSentiences, component.MaxSentiences);

        var groups = new HashSet<string>();

        for (var i = 0; i < toMakeSentient && targetList.Count > 0; i++)
        {
            // weighted random to pick a sentience target
            var totalWeight = targetList.Sum(x => x.Comp.Weight);
            // This initial target should never be picked.
            // It's just so that target doesn't need to be nullable and as a safety fallback for id floating point errors ever mess up the comparison in the foreach.
            var target = targetList[0];
            var chosenWeight = _random.NextFloat(totalWeight);
            var currentWeight = 0.0;
            foreach (var potentialTarget in targetList)
            {
                currentWeight += potentialTarget.Comp.Weight;
                if (currentWeight > chosenWeight)
                {
                    target = potentialTarget;
                    break;
                }
            }
            targetList.Remove(target);

            var name = MetaData(target).EntityName;
            RemComp<SentienceTargetComponent>(target);
            _ghostRole.CreateGhostRole(target.Owner,
                name: name,
                description: Loc.GetString("station-event-random-sentience-role-description", ("name", name)),
                rules: Loc.GetString(GhostRoleComponent.DefaultRules));
            groups.Add(Loc.GetString(target.Comp.FlavorKind));
        }

        if (groups.Count == 0)
            return;

        var groupList = groups.ToList();
        var kind1 = groupList.Count > 0 ? groupList[0] : "???";
        var kind2 = groupList.Count > 1 ? groupList[1] : "???";
        var kind3 = groupList.Count > 2 ? groupList[2] : "???";

        ChatSystem.DispatchStationAnnouncement(
            station.Value,
            Loc.GetString("station-event-random-sentience-announcement",
                ("kind1", kind1), ("kind2", kind2), ("kind3", kind3), ("amount", groupList.Count),
                ("data", _random.Pick(ProtoMan.Index(DataSourceNames))),
                ("strength", _random.Pick(ProtoMan.Index(IntelligenceLevelNames)))
            ),
            playDefaultSound: false,
            colorOverride: Color.Gold
        );
    }
}
