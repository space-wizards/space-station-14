using System.Linq;
using System.Numerics;
using Content.Shared.CCVar;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Decals;
using Content.Shared.Tiles;
using Robust.Shared.Configuration;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Maps;

/// <summary>
///     Handles server-side tile manipulation like prying/deconstructing tiles.
/// </summary>
public sealed class TileSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private readonly SharedDecalSystem _decal = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public const int ChunkSize = 16;

    private int _tileStackLimit;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GridInitializeEvent>(OnGridStartup);
        SubscribeLocalEvent<TileHistoryComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<TileHistoryComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<TileHistoryComponent, FloorTileAttemptEvent>(OnFloorTileAttempt);

        _cfg.OnValueChanged(CCVars.TileStackLimit, t => _tileStackLimit = t, true);
    }

    private void OnHandleState(EntityUid uid, TileHistoryComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not TileHistoryState state && args.Current is not TileHistoryDeltaState)
            return;

        if (args.Current is TileHistoryState fullState)
        {
            component.ChunkHistory.Clear();
            foreach (var (key, value) in fullState.ChunkHistory)
            {
                component.ChunkHistory[key] = new TileHistoryChunk(value);
            }

            return;
        }

        if (args.Current is TileHistoryDeltaState deltaState)
        {
            deltaState.ApplyToComponent(component);
        }
    }

    private void OnGetState(EntityUid uid, TileHistoryComponent component, ref ComponentGetState args)
    {
        if (args.FromTick <= component.CreationTick || args.FromTick <= component.ForceTick)
        {
            var fullHistory = new Dictionary<Vector2i, TileHistoryChunk>(component.ChunkHistory.Count);
            foreach (var (key, value) in component.ChunkHistory)
            {
                fullHistory[key] = new TileHistoryChunk(value);
            }
            args.State = new TileHistoryState(fullHistory);
            return;
        }

        var data = new Dictionary<Vector2i, TileHistoryChunk>();
        foreach (var (index, chunk) in component.ChunkHistory)
        {
            if (chunk.LastModified >= args.FromTick)
                data[index] = new TileHistoryChunk(chunk);
        }

        args.State = new TileHistoryDeltaState(data, new(component.ChunkHistory.Keys));
    }

    /// <summary>
    /// On grid startup, ensure that we have Tile History.
    /// </summary>
    private void OnGridStartup(GridInitializeEvent ev)
    {
        if (HasComp<MapComponent>(ev.EntityUid))
            return;

        EnsureComp<TileHistoryComponent>(ev.EntityUid);
    }

    /// <summary>
    ///     Returns a weighted pick of a tile variant.
    /// </summary>
    public byte PickVariant(ContentTileDefinition tile)
    {
        return PickVariant(tile, _robustRandom.GetRandom());
    }

    /// <summary>
    ///     Returns a weighted pick of a tile variant.
    /// </summary>
    public byte PickVariant(ContentTileDefinition tile, int seed)
    {
        var rand = new System.Random(seed);
        return PickVariant(tile, rand);
    }

    /// <summary>
    ///     Returns a weighted pick of a tile variant.
    /// </summary>
    public byte PickVariant(ContentTileDefinition tile, System.Random random)
    {
        var variants = tile.PlacementVariants;

        var sum = variants.Sum();
        var accumulated = 0f;
        var rand = random.NextFloat() * sum;

        for (byte i = 0; i < variants.Length; ++i)
        {
            accumulated += variants[i];

            if (accumulated >= rand)
                return i;
        }

        // Shouldn't happen
        throw new InvalidOperationException($"Invalid weighted variantize tile pick for {tile.ID}!");
    }

    /// <summary>
    ///     Returns a tile with a weighted random variant.
    /// </summary>
    public Tile GetVariantTile(ContentTileDefinition tile, System.Random random)
    {
        return new Tile(tile.TileId, variant: PickVariant(tile, random));
    }

    /// <summary>
    ///     Returns a tile with a weighted random variant.
    /// </summary>
    public Tile GetVariantTile(ContentTileDefinition tile, int seed)
    {
        var rand = new System.Random(seed);
        return new Tile(tile.TileId, variant: PickVariant(tile, rand));
    }

    public bool PryTile(Vector2i indices, EntityUid gridId)
    {
        var grid = Comp<MapGridComponent>(gridId);
        var tileRef = _maps.GetTileRef(gridId, grid, indices);
        return PryTile(tileRef);
    }

    public bool PryTile(TileRef tileRef)
    {
        return PryTile(tileRef, false);
    }

    public bool PryTile(TileRef tileRef, bool pryPlating)
    {
        var tile = tileRef.Tile;

        if (tile.IsEmpty)
            return false;

        var tileDef = (ContentTileDefinition)_tileDefinitionManager[tile.TypeId];

        if (!tileDef.CanCrowbar)
            return false;

        return DeconstructTile(tileRef);
    }

    public bool ReplaceTile(TileRef tileref, ContentTileDefinition replacementTile)
    {
        if (!TryComp<MapGridComponent>(tileref.GridUid, out var grid))
            return false;
        return ReplaceTile(tileref, replacementTile, tileref.GridUid, grid);
    }

    public bool ReplaceTile(TileRef tileref, ContentTileDefinition replacementTile, EntityUid grid, MapGridComponent? component = null, byte? variant = null)
    {
        DebugTools.Assert(tileref.GridUid == grid);

        if (!Resolve(grid, ref component))
            return false;

        var key = tileref.GridIndices;
        var currentTileDef = (ContentTileDefinition) _tileDefinitionManager[tileref.Tile.TypeId];

        // If the tile we're placing has a baseTurf that matches the tile we're replacing, we don't need to create a history
        // unless the tile already has a history.
        var history = EnsureComp<TileHistoryComponent>(grid);
        var chunkIndices = SharedMapSystem.GetChunkIndices(key, ChunkSize);
        history.ChunkHistory.TryGetValue(chunkIndices, out var chunk);
        var historyExists = chunk != null && chunk.History.ContainsKey(key);

        if (replacementTile.BaseTurf != currentTileDef.ID || historyExists)
        {
            if (chunk == null)
            {
                chunk = new TileHistoryChunk();
                history.ChunkHistory[chunkIndices] = chunk;
            }

            chunk.LastModified = _timing.CurTick;
            Dirty(grid, history);

            //Create stack if needed
            if (!chunk.History.TryGetValue(key, out var stack))
            {
                stack = new List<ProtoId<ContentTileDefinition>>();
                chunk.History[key] = stack;
            }

            //Prevent the doomstack
            if (stack.Count >= _tileStackLimit && _tileStackLimit != 0)
                return false;

            //Push current tile to the stack, if not empty
            if (!tileref.Tile.IsEmpty)
            {
                stack.Add(currentTileDef.ID);
            }
        }

        variant ??= PickVariant(replacementTile);
        var decals = _decal.GetDecalsInRange(tileref.GridUid, _turf.GetTileCenter(tileref).Position, 0.5f);
        foreach (var (id, _) in decals)
        {
            _decal.RemoveDecal(tileref.GridUid, id);
        }

        _maps.SetTile(grid, component, tileref.GridIndices, new Tile(replacementTile.TileId, 0, variant.Value));
        return true;
    }


    public bool DeconstructTile(TileRef tileRef, bool spawnItem = true)
    {
        if (tileRef.Tile.IsEmpty)
            return false;

        var tileDef = (ContentTileDefinition)_tileDefinitionManager[tileRef.Tile.TypeId];

        //Can't deconstruct anything that doesn't have a base turf.
        if (tileDef.BaseTurf == null)
            return false;

        var gridUid = tileRef.GridUid;
        var mapGrid = Comp<MapGridComponent>(gridUid);

        const float margin = 0.1f;
        var bounds = mapGrid.TileSize - margin * 2;
        var indices = tileRef.GridIndices;
        var coordinates = _maps.GridTileToLocal(gridUid, mapGrid, indices)
            .Offset(new Vector2(
                (_robustRandom.NextFloat() - 0.5f) * bounds,
                (_robustRandom.NextFloat() - 0.5f) * bounds));

        var historyComp = EnsureComp<TileHistoryComponent>(gridUid);
        ProtoId<ContentTileDefinition> previousTileId;

        var chunkIndices = SharedMapSystem.GetChunkIndices(indices, ChunkSize);

        //Pop from stack if we have history
        if (historyComp.ChunkHistory.TryGetValue(chunkIndices, out var chunk) &&
            chunk.History.TryGetValue(indices, out var stack) && stack.Count > 0)
        {
            chunk.LastModified = _timing.CurTick;
            Dirty(gridUid, historyComp);

            previousTileId = stack.Last();
            stack.RemoveAt(stack.Count - 1);

            //Clean up empty stacks to avoid memory buildup
            if (stack.Count == 0)
            {
                chunk.History.Remove(indices);
            }

            // Clean up empty chunks
            if (chunk.History.Count == 0)
            {
                historyComp.ChunkHistory.Remove(chunkIndices);
            }
        }
        else
        {
            //No stack? Assume BaseTurf was the layer below
            previousTileId = tileDef.BaseTurf.Value;
        }

        if (spawnItem)
        {
            //Actually spawn the relevant tile item at the right position and give it some random offset.
            var tileItem = Spawn(tileDef.ItemDropPrototypeName, coordinates);
            Transform(tileItem).LocalRotation = _robustRandom.NextDouble() * Math.Tau;
        }

        //Destroy any decals on the tile
        var decals = _decal.GetDecalsInRange(gridUid, coordinates.SnapToGrid(EntityManager, _mapManager).Position, 0.5f);
        foreach (var (id, _) in decals)
        {
            _decal.RemoveDecal(tileRef.GridUid, id);
        }

        //Replace tile with the one it was placed on
        var previousDef = (ContentTileDefinition)_tileDefinitionManager[previousTileId];
        _maps.SetTile(gridUid, mapGrid, indices, new Tile(previousDef.TileId));

        return true;
    }

    private void OnFloorTileAttempt(Entity<TileHistoryComponent> ent, ref FloorTileAttemptEvent args)
    {
        if (_tileStackLimit == 0)
            return;
        var chunkIndices = SharedMapSystem.GetChunkIndices(args.GridIndices, ChunkSize);
        if (!ent.Comp.ChunkHistory.TryGetValue(chunkIndices, out var chunk) ||
            !chunk.History.TryGetValue(args.GridIndices, out var stack))
            return;
        args.Cancelled = stack.Count >= _tileStackLimit; // greater or equals because the attempt itself counts as a tile we're trying to place
    }
}
