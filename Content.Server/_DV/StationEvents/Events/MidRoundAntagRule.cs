using Content.Server.Antag;
using Content.Server.StationEvents.Components;
using Robust.Shared.Map;

namespace Content.Server.StationEvents.Events;

// Impstation heads up, this is MIT code, not AGPL. Licenced under Nyanotrasen namespace

/// <summary>
/// Makes antags spawn at a random midround antag or vent critter spawner.
/// </summary>
public sealed class MidRoundAntagRule : StationEventSystem<MidRoundAntagRuleComponent>
{
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MidRoundAntagRuleComponent, AntagSelectLocationEvent>(OnSelectLocation);
    }

    private void OnSelectLocation(Entity<MidRoundAntagRuleComponent> ent, ref AntagSelectLocationEvent args)
    {
        if (!TryGetRandomStation(out var station))
            return;

        var spawns = FindSpawns(station.Value);
        if (spawns.Count == 0)
        {
            Log.Warning($"Couldn't find any suitable midround antag spawners for {ToPrettyString(ent):rule}");
            return;
        }

        args.Coordinates.AddRange(spawns);
    }

    private List<MapCoordinates> FindSpawns(EntityUid station)
    {
        var spawns = new List<MapCoordinates>();
        var query = EntityQueryEnumerator<MidRoundAntagSpawnLocationComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (StationSystem.GetOwningStation(uid, xform) == station && xform.GridUid != null)
                spawns.Add(_xform.GetMapCoordinates(xform));
        }

        // if there are any midround antag spawns mapped, use them
        if (spawns.Count > 0)
            return spawns;

        // otherwise, fall back to vent critter spawns
        Log.Info($"Station {ToPrettyString(station):station} has no midround antag spawnpoints mapped, falling back. Please map them!");
        var fallbackQuery = EntityQueryEnumerator<VentCritterSpawnLocationComponent, TransformComponent>();
        while (fallbackQuery.MoveNext(out var uid, out _, out var xform))
        {
            if (StationSystem.GetOwningStation(uid, xform) == station && xform.GridUid != null)
                spawns.Add(_xform.GetMapCoordinates(xform));
        }

        return spawns;
    }
}
