using Content.Shared.Tiles;
using Robust.Shared.Map.Components;

namespace Content.Server.Tiles;

public sealed partial class RequiresTileSystem : EntitySystem
{
    /*
     * Needs to be on server as client can't predict QueueDel.
     */

    [Dependency] private SharedMapSystem _maps = default!;
    [Dependency] private EntityQuery<RequiresTileComponent> _tilesQuery = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TileChangedEvent>(OnTileChange);
    }

    private void OnTileChange(ref TileChangedEvent ev)
    {
        if (!TryComp<MapGridComponent>(ev.Entity, out var grid))
            return;

        foreach (var change in ev.Changes)
        {
            var anchored = _maps.GetAnchoredEntitiesEnumerator(ev.Entity, grid, change.GridIndices);

            while (anchored.MoveNext(out var ent))
            {
                if (!_tilesQuery.HasComponent(ent.Value))
                    continue;

                QueueDel(ent.Value);
            }
        }
    }
}
