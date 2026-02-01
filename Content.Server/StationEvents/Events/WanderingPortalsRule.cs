using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
using Content.Shared.Storage;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;
using Robust.Shared.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Content.Server.StationEvents.Events;

public sealed class WanderingPortalsRule : StationEventSystem<WanderingPortalsRuleComponent>
{
    [Dependency] private readonly LinkedEntitySystem _linker = default!;

    protected override void Started(EntityUid uid, WanderingPortalsRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetRandomStation(out var station))
        {
            return;
        }

        // Just use the vent spawn locations ... they're already pretty good, and the portals will swiftly move on.
        var locations = EntityQueryEnumerator<VentCritterSpawnLocationComponent, TransformComponent>();
        var validLocations = new List<EntityCoordinates>();
        while (locations.MoveNext(out _, out _, out var transform))
        {
            if (CompOrNull<StationMemberComponent>(transform.GridUid)?.Station == station)
            {
                validLocations.Add(transform.Coordinates);
            }
        }

        if (validLocations.Count <= 0)
        {
            return;
        }

        var portals = new List<EntityUid>();
        var portalAmount = RobustRandom.Next(component.MinPortals, component.MaxPortals);
        RobustRandom.Shuffle(validLocations);
        for (var i = 0; i < portalAmount && i < validLocations.Count; i++)
        {
            var portal = Spawn(component.PortalPrototype, validLocations.ElementAt(i));
            portals.Add(portal);
            if (TryComp<PortalComponent>(portal, out var teleportal))
                teleportal.IgnoreStationaryObjects = component.IgnoreStationaryObjects;
        }

        RobustRandom.Shuffle(portals);
        for (var i = 0; i < (portals.Count % 2 == 1 ? portals.Count - 1 : portals.Count); i += 2)
        {
            _linker.TryLink(portals.ElementAt(i), portals.ElementAt(i + 1));
        }
    }
}
