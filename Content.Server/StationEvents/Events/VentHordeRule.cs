using System.Linq;
using Content.Server.Pinpointer;
using Content.Server.StationEvents.Components;
using Content.Server.VentHorde.Components;
using Content.Server.VentHorde.Systems;
using Content.Shared.EntityTable;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.StationEvents.Events;

/// <summary>
/// Variant of <see cref="VentCrittersRule"/> that selects a single vent and spawns all entities there.
/// </summary>
public sealed class VentHordeRule : StationEventSystem<VentHordeRuleComponent>
{
    /*
     * DO NOT COPY PASTE THIS TO MAKE YOUR MOB EVENT.
     * USE THE PROTOTYPE.
     */

    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityTableSystem _table = default!;
    [Dependency] private readonly VentHordeSystem _horde = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    protected override void Added(EntityUid uid, VentHordeRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        // Choose location and make sure it's not null
        component.ChosenVent = ChooseVent();

        if (component.ChosenVent is not { } vent)
        {
            Log.Warning($"Unable to find a valid vent for {args.RuleId}!");
            ForceEndSelf(uid, gameRule);
            return;
        }

        // Get the event component so we can format the announcement
        if (TryComp<StationEventComponent>(uid, out var stationEventComp) && stationEventComp.StartAnnouncement != null)
        {
            // Get the nearest beacon
            var mapLocation = _transform.ToMapCoordinates(Transform(vent).Coordinates);
            var nearestBeacon = _navMap.GetNearestBeaconString(mapLocation, onlyName: true);

            // Format the announcement with the location, if the string doesn't have them it'll still work fine
            // time is not said on purpose to keep the players on their toes.
            // also because we cannot tell the end time inside of Added().
            stationEventComp.StartAnnouncement =
                Loc.GetString(stationEventComp.StartAnnouncement,
                    ("location", nearestBeacon));
        }

        base.Added(uid, component, gameRule, args);
    }

    protected override void Started(EntityUid uid, VentHordeRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!Exists(component.ChosenVent))
        {
            Log.Warning($"Chosen vent for {args.RuleId} does not exist!");
            ForceEndSelf(uid, gameRule);
            return;
        }

        if (!TryComp<StationEventComponent>(uid, out var stationEventComp))
            return;

        // We grab when the gamerule is expected to end and subtract the current time from it to get the duration.
        var duration = (stationEventComp.EndTime - _timing.CurTime) ?? TimeSpan.Zero;

        var spawns = _table.GetSpawns(component.Table);

        if (component.ChosenVent == null)
            return;

        // And start the spawn at the chosen vent.
        // The duration is the same as the time until expected gamerule end time, but that is only for convenience.
        // The spawn can happen early in certain circumstances anyway.
        _horde.StartHordeSpawn(component.ChosenVent.Value, spawns.ToList(), duration);
    }

    private EntityUid? ChooseVent()
    {
        // Get a station
        if (!TryGetRandomStation(out var station))
        {
            return null;
        }

        // Query the possible locations
        var locations = EntityQueryEnumerator<VentCritterSpawnLocationComponent, TransformComponent>();
        var validLocations = new List<EntityUid>();

        // Filter to things on the same station
        while (locations.MoveNext(out var uid, out _, out var transform))
        {
            if (!transform.Anchored)
                continue;

            if (HasComp<VentHordeSpawnerComponent>(uid))
                continue;

            if (CompOrNull<StationMemberComponent>(transform.GridUid)?.Station == station)
            {
                validLocations.Add(uid);
            }
        }

        // Pick one at random
        if (validLocations.Count != 0)
            return _random.Pick(validLocations);

        return null;
    }
}
