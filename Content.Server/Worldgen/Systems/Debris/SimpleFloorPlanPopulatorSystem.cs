using Content.Server.Worldgen.Components.Debris;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.Worldgen.Systems.Debris;

/// <summary>
///     This handles populating simple structures, simply using a loot table for each tile.
/// </summary>
public sealed class SimpleFloorPlanPopulatorSystem : BaseWorldSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinition = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        SubscribeLocalEvent<SimpleFloorPlanPopulatorComponent, LocalStructureLoadedEvent>(OnFloorPlanBuilt);
    }

    private void OnFloorPlanBuilt(EntityUid uid, SimpleFloorPlanPopulatorComponent component,
        LocalStructureLoadedEvent args)
    {
        var placeables = new List<string?>(4);
        var grid = Comp<MapGridComponent>(uid);
        var enumerator = _map.GetAllTilesEnumerator(uid, grid);
        while (enumerator.MoveNext(out var tile))
        {
            var coords = grid.GridTileToLocal(tile.Value.GridIndices);
            var selector = tile.Value.Tile.GetContentTileDefinition(_tileDefinition).ID;
            if (!component.Caches.TryGetValue(selector, out var cache))
                continue;

            placeables.Clear();
            cache.GetSpawns(_random, ref placeables);

            foreach (var proto in placeables)
            {
                if (proto is null)
                    continue;

                Spawn(proto, coords);
            }
        }
    }
}

