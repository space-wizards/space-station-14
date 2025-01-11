using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;
using Content.Server.Traits.Assorted;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Content.Shared.Random.Rules;
using Content.Server.Station.Components;

namespace Content.Server.StationEvents.Events;

public sealed class MassHallucinationsRule : StationEventSystem<MassHallucinationsRuleComponent>
{
    [Dependency] private readonly ParacusiaSystem _paracusia = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    protected override void Started(EntityUid uid, MassHallucinationsRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
{
    base.Started(uid, component, gameRule, args);
    var query = EntityQueryEnumerator<MindContainerComponent>();

    if (component.SweetwaterOnly)
    {
        // Get the mapId from the station (or another valid source for the map)
        if (TryComp<StationDataComponent>(uid, out var stationData))
        {
            foreach (var grid in stationData.Grids)
            {
                // Using the grid directly to get the MapId
                var mapId = Transform(grid).MapID;

                // Get all grids on that map
                foreach (var gridEntity in _mapManager.GetAllGrids(mapId))
                {
                    if (TryComp<SweetwaterComponent>(gridEntity.Owner, out var sweetwaterComp))
                    {
                        ApplyOceanSound(gridEntity.Owner, component);
                    }
                }
            }
        }
    }
    else
    {
        while (query.MoveNext(out var ent, out _))
        {
            if (!HasComp<ParacusiaComponent>(ent))
            {
                EnsureComp<MassHallucinationsComponent>(ent);
                var paracusia = EnsureComp<ParacusiaComponent>(ent);
                _paracusia.SetSounds(ent, component.Sounds, paracusia);
                _paracusia.SetTime(ent, component.MinTimeBetweenIncidents, component.MaxTimeBetweenIncidents, paracusia);
                _paracusia.SetDistance(ent, component.MaxSoundDistance);
            }
        }
    }
}


    private void ApplyOceanSound(EntityUid map, MassHallucinationsRuleComponent component)
    {
        foreach (var ent in GetGridChildren(map))
        {
            if (HasComp<MindContainerComponent>(ent) && !HasComp<ParacusiaComponent>(ent))
            {
                EnsureComp<MassHallucinationsComponent>(ent);
                var paracusia = EnsureComp<ParacusiaComponent>(ent);
                _paracusia.SetSounds(ent, component.Sounds, paracusia);
                _paracusia.SetTime(ent, component.MinTimeBetweenIncidents, component.MaxTimeBetweenIncidents, paracusia);
                _paracusia.SetDistance(ent, component.MaxSoundDistance);
            }
        }
    }

    private IEnumerable<EntityUid> GetGridChildren(EntityUid target)
    {
        if (TryComp<StationDataComponent>(target, out var station))
        {
            foreach (var grid in station.Grids)
            {
                var enumerator = Transform(grid).ChildEnumerator; // Non-generic Transform
                while (enumerator.MoveNext(out var ent))
                {
                    yield return ent;
                }
            }
        }
    }

    protected override void Ended(EntityUid uid, MassHallucinationsRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);
        var query = EntityQueryEnumerator<MassHallucinationsComponent>();
        while (query.MoveNext(out var ent, out _))
        {
            RemComp<ParacusiaComponent>(ent);
        }
    }
}
