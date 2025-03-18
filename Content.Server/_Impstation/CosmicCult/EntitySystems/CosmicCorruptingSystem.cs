using System.Linq;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;
using Content.Server._Impstation.CosmicCult.Components;

namespace Content.Server._Impstation.CosmicCult.EntitySystems;
public sealed class CosmicCorruptingSystem : EntitySystem
{
    [Dependency] private readonly ITileDefinitionManager _tileDefinition = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly TurfSystem _turfs = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _rand = default!;

    private readonly HashSet<Vector2i> _neighbourPositions =
    [
        new Vector2i(-1, 1),
        new Vector2i(0, 1),
        new Vector2i(1, 1),
        new Vector2i(-1, 0),
        new Vector2i(0,0),
        new Vector2i(1, 0),
        new Vector2i(-1, -1),
        new Vector2i(0, -1),
        new Vector2i(1, -1),
    ];

    /// <remarks>
    /// this system is a mostly generic way of replacing tiles around an entity. the only hardcoded behaviour is secret walls -> malign doors, but that shouldn't be too hard to fix if this is needed for smth else later.
    /// </remarks>
    public override void Initialize()
    {
        SubscribeLocalEvent<CosmicCorruptingComponent, MapInitEvent>(OnMapInit);
    }

    //when the entity spawns, add all neighbouring tiles to the corruptable list
    private void OnMapInit(Entity<CosmicCorruptingComponent> ent, ref MapInitEvent args)
    {
        RecalculateStartingTiles(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var blanktimer = EntityQueryEnumerator<CosmicCorruptingComponent>();
        while (blanktimer.MoveNext(out var uid, out var comp))
        {
            if (comp.Enabled && _timing.CurTime >= comp.CorruptionTimer)
            {
                comp.CorruptionTimer = _timing.CurTime + comp.CorruptionSpeed;
                ConvertTiles((uid, comp));
                if (comp.CorruptionTicks <= comp.CorruptionMaxTicks)
                {
                    comp.CorruptionTicks++;
                    comp.CorruptionChance -= comp.CorruptionReduction;
                }

                if (comp.CorruptionTicks >= comp.CorruptionMaxTicks && comp.AutoDisable)
                    comp.Enabled = false; //maybe just remComp this? atm nothing re-enables a corruptor so that should be safe to do?
            }
        }
    }

    private void ConvertTiles(Entity<CosmicCorruptingComponent> ent)
    {
        var xform = Transform(ent);
        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var mapGrid))
            return;

        var convertTile = (ContentTileDefinition)_tileDefinition[ent.Comp.ConversionTile];

        //if this is a mobile corruptor, reset the list of corruptable tiles every attempt.
        //not a super clean solution because I didn't account for the astral nova in the first rewrite but it works well enough for our purposes.
        if (ent.Comp.Mobile)
        {
            RecalculateStartingTiles(ent);
        }

        //go over every corruptible tile
        foreach (var pos in new HashSet<Vector2i>(ent.Comp.CorruptableTiles)) //we love avoiding ConcurrentModificationExceptions
        {
            var tileRef = _map.GetTileRef((gridUid, mapGrid), pos);
            if (tileRef.Tile.TypeId == convertTile.TileId || tileRef.Tile.IsEmpty) //if it's already corrupted (or space), remove it from the list and continue
            {
                ent.Comp.CorruptableTiles.Remove(pos);
                continue;
            }

            if (_rand.Prob(ent.Comp.CorruptionChance)) //if it rolls good
            {
                //replace & variantise the tile
                _tile.ReplaceTile(tileRef, convertTile);
                _tile.PickVariant(convertTile);

                //then add the new neighbours as targets as long as they're not already corrupted
                foreach (var neighbourPos in _neighbourPositions)
                {
                    var neighbourRef = _map.GetTileRef((gridUid, mapGrid), tileRef.GridIndices + neighbourPos);
                    if (neighbourRef.Tile.TypeId == convertTile.TileId || tileRef.Tile.IsEmpty) //ignore already corrupted (or space) tiles
                        continue;

                    ent.Comp.CorruptableTiles.Add(neighbourRef.GridIndices);
                }

                //corrupt anything that can be corrupted
                foreach (var convertedEnt in _map.GetAnchoredEntities((gridUid, mapGrid), pos).ToList())
                {
                    var proto = Prototype(convertedEnt);
                    if (ent.Comp.EntityConversionDict.TryGetValue(proto?.ID!, out var conversion))
                    {
                        Spawn(conversion, Transform(convertedEnt).Coordinates);
                        QueueDel(convertedEnt);
                    }
                }

                //spawn the vfx if we should
                if (ent.Comp.UseVFX)
                    Spawn(ent.Comp.TileConvertVFX, _turfs.GetTileCenter(tileRef));

                ent.Comp.CorruptableTiles.Remove(pos);
            }
        }
    }

    #region API

    public void SetCorruptionTime(Entity<CosmicCorruptingComponent> ent, TimeSpan time)
    {
        ent.Comp.CorruptionSpeed = time;
    }

    public void Enable(Entity<CosmicCorruptingComponent> ent, bool recalculate = true)
    {
        ent.Comp.Enabled = true;

        if (recalculate)
            RecalculateStartingTiles(ent);
    }

    public void Disable(Entity<CosmicCorruptingComponent> ent)
    {
        ent.Comp.Enabled = false;
    }

    public void RecalculateStartingTiles(Entity<CosmicCorruptingComponent> ent)
    {
        ent.Comp.CorruptableTiles.Clear();

        var xform = Transform(ent);

        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var mapGrid))
            return;

        var grid = (gridUid, mapGrid);
        var tile = _map.GetTileRef(grid, xform.Coordinates);

        if (ent.Comp.FloodFillStarting) //todo make this async? it doesn't actually run that much though
        {
            var convertTile = (ContentTileDefinition)_tileDefinition[ent.Comp.ConversionTile];
            var visitedTiles = new HashSet<Vector2i>();
            var tilesToVisit = new HashSet<Vector2i>();

            tilesToVisit.Add(tile.GridIndices);

            int count = 0;

            while (tilesToVisit.Count > 0)
            {
                //get the first tile in the list
                var currtile = tilesToVisit.First();
                count++;

                //check every neighbouring tile
                foreach (var neighbourPos in _neighbourPositions)
                {
                    var neighbourRef = _map.GetTileRef((gridUid, mapGrid), currtile + neighbourPos);

                    //if it's already been converted
                    if (neighbourRef.Tile.TypeId == convertTile.TileId)
                    {
                        //and not already visited
                        if (!visitedTiles.Contains(neighbourRef.GridIndices))
                            tilesToVisit.Add(neighbourRef.GridIndices); //add it to the to visit list
                    }
                    else
                    {
                        //else, it's not been converted, so mark it as visited and add it to the corruptible tiles list
                        //we don't care if the tile is empty, that'll get checked later
                        visitedTiles.Add(neighbourRef.GridIndices);
                        ent.Comp.CorruptableTiles.Add(neighbourRef.GridIndices);
                    }
                }

                //finally, mark the tile as visited and remove it from the toVisit list
                visitedTiles.Add(currtile);
                tilesToVisit.Remove(currtile);
            }

            Log.Info($"floodfill tile recaulculation ran {count} times");
        }
        else
        {
            //add every neighbouring tile to the corruptable list
            //don't bother checking eligibility at this point because it'll get done later anyway
            foreach (var neighbourPos in _neighbourPositions)
            {
                var neighbourRef = _map.GetTileRef((gridUid, mapGrid), tile.GridIndices + neighbourPos);

                ent.Comp.CorruptableTiles.Add(neighbourRef.GridIndices);
            }
        }
    }

    #endregion
}
