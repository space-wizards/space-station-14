using Content.Shared.Maps;
using Robust.Shared.Map;

namespace Content.Shared.Tiles;

/// <summary>
/// This handles...
/// </summary>
public sealed class TileStackSystem : EntitySystem
{
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GridInitializeEvent>(OnGridInit);
        SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoved);
    }

    private void OnGridInit(GridInitializeEvent ev)
    {
        EnsureComp<TileStackMapComponent>(ev.EntityUid);
    }

    private void OnGridRemoved(GridRemovalEvent ev)
    {
        RemComp<TileStackMapComponent>(ev.EntityUid);
    }

    public bool HasTileStack(TileRef tileRef)
    {
        return HasTileStack(tileRef.GridIndices, tileRef.GridUid);
    }

    public bool HasTileStack(Vector2i gridIndices, EntityUid gridUid)
    {
        if (!TryComp<TileStackMapComponent>(gridUid, out var tileStackMap))
            return false;
        return tileStackMap.Data.ContainsKey(gridIndices);
    }

    /// <summary>
    ///     Creates a tilestack at the location of the tile ref.
    /// </summary>
    public void CreateTileStack(TileRef tileRef)
    {
        var tilestack = new List<string>();
        var curtile = tileRef.GetContentTileDefinition().ID;
        while (!string.IsNullOrEmpty(curtile))
        {
            tilestack.Insert(0, _tileDefinitionManager[curtile].ID);
            curtile = ((ContentTileDefinition) _tileDefinitionManager[curtile]).BaseTurf;
        }
        if (!TryComp<TileStackMapComponent>(tileRef.GridUid, out var tileStackMap))
            return;
        tileStackMap.Data.Add(tileRef.GridIndices, tilestack);
    }
}
