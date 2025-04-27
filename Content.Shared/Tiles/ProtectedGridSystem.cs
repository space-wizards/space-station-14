using System.Linq;
using Robust.Shared.Map.Components;

namespace Content.Shared.Tiles;

public sealed class ProtectedGridSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ProtectedGridComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ProtectedGridComponent, FloorTileAttemptEvent>(OnFloorTileAttempt);
    }

    private void OnMapInit(Entity<ProtectedGridComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<MapGridComponent>(ent, out var grid))
            return;

        ent.Comp.BaseIndices = _map.GetAllTiles(ent, grid, ignoreEmpty: true).Select(t => t.GridIndices).ToHashSet();
        Dirty(ent);
    }

    private void OnFloorTileAttempt(Entity<ProtectedGridComponent> ent, ref FloorTileAttemptEvent args)
    {
        if (!ent.Comp.BaseIndices.Contains(args.GridIndices))
            args.Cancelled = true;
    }
}
