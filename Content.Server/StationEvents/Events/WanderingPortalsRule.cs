using System.Linq;
using Content.Server.Anomaly;
using Content.Server.Station.Systems;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;

namespace Content.Server.StationEvents.Events;

public sealed class WanderingPortalsRule : StationEventSystem<WanderingPortalsRuleComponent>
{
    [Dependency] private readonly LinkedEntitySystem _linkerSystem = default!;
    [Dependency] private readonly AnomalySystem _anomalySystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;

    protected override void Started(EntityUid uid, WanderingPortalsRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (TryGetRandomStation(out var station) && station.HasValue && _stationSystem.GetLargestGrid(station.Value) is { } grid)
        {
            var basicEnabled = component.AllowBasic || !component.AllowGravity;
            var wellPortals = new List<EntityUid>();
            var spoutPortals = new List<EntityUid>();
            var basicPortals = new List<EntityUid>();
            var basicPortalAmount = RobustRandom.Next(component.MinBasicPortals, component.MaxBasicPortals);
            var gravPortalPairAmount = RobustRandom.Next(basicEnabled ? component.MinGravPortalPairsWhenBasic : component.MinGravPortalPairs, component.MaxGravPortalPairs);
            basicPortalAmount -= (component.AllowOdd ? 0 : basicPortalAmount % 2) + (component.AllowGravity ? gravPortalPairAmount * component.GravPortalPairCost : 0);
            basicPortalAmount = basicEnabled ? basicPortalAmount : 0;
            gravPortalPairAmount = component.AllowGravity ? gravPortalPairAmount : 0;

            for (var i = 0; i < basicPortalAmount; i++)
            {
                // Use the anomaly spawns ... these portals are practically anomalies anyway
                var portal = _anomalySystem.SpawnOnRandomGridLocation(grid, component.PortalPrototype);
                if (portal != null)
                    basicPortals.Add(portal.Value);
                if (TryComp<PortalComponent>(portal, out var teleportal))
                    teleportal.IgnoreStationaryObjects = component.IgnoreStationaryObjects;
            }

            for (var i = 0; i < gravPortalPairAmount; i++)
            {
                // Use the anomaly spawns ... these portals are practically anomalies anyway
                var well = _anomalySystem.SpawnOnRandomGridLocation(grid, component.GravityWellPrototype);
                var spout = _anomalySystem.SpawnOnRandomGridLocation(grid, component.GravitySpoutPrototype);
                if (well != null)
                    wellPortals.Add(well.Value);
                if (spout != null)
                    spoutPortals.Add(spout.Value);
            }

            RobustRandom.Shuffle(wellPortals);
            RobustRandom.Shuffle(spoutPortals);
            RobustRandom.Shuffle(basicPortals);

            for (var i = 0; i < (basicPortals.Count % 2 == 1 ? basicPortals.Count - 1 : basicPortals.Count); i += 2)
            {
                _linkerSystem.TryLink(basicPortals.ElementAt(i), basicPortals.ElementAt(i + 1));
            }

            for (var i = 0; i < gravPortalPairAmount; i++)
            {
                _linkerSystem.TryLink(wellPortals.ElementAt(i), spoutPortals.ElementAt(i));
            }
        }
    }
}
