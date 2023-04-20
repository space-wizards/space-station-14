using Robust.Shared.Map.Components;
using Robust.Shared.Map.Enumerators;

namespace Content.Shared.Tiles;

public sealed class RequiresTileSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TileChangedEvent>(OnTileChange);
    }

    private void OnTileChange(ref TileChangedEvent ev)
    {
        if (!TryComp<MapGridComponent>(ev.Entity, out var grid))
            return;

        var anchored = grid.GetAnchoredEntitiesEnumerator(ev.NewTile.GridIndices);
        if (anchored.Equals(AnchoredEntitiesEnumerator.Empty))
            return;

        var query = GetEntityQuery<RequiresTileComponent>();

        while (anchored.MoveNext(out var ent))
        {
            if (!query.HasComponent(ent.Value))
                continue;

            QueueDel(ent.Value);
        }
    }
}
