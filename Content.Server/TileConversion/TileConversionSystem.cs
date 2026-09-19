using System.Linq;
using Content.Shared.Maps;
using Content.Shared.Trigger;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.TileConversion;

public sealed partial class TileConversionSystem : EntitySystem
{
    [Dependency] private MapSystem _map = default!;

    private readonly HashSet<Vector2i> _neighbourPositions =
    [
        new(-1, 1),
        new(0, 1),
        new(1, 1),
        new(-1, 0),
        new(0, 0),
        new(1, 0),
        new(-1, -1),
        new(0, -1),
        new(1, -1),
    ];

    [Dependency] private IRobustRandom _rand = default!;
    [Dependency] private TileSystem _tile = default!;
    [Dependency] private ITileDefinitionManager _tileDefinition = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private TurfSystem _turfs = default!;

    /// <remarks>
    ///     this system is a mostly generic way of replacing tiles around an entity. the only hardcoded behaviour is secret
    ///     walls -> malign doors, but that shouldn't be too hard to fix if this is needed for smth else later.
    /// </remarks>
    public override void Initialize()
    {
        SubscribeLocalEvent<TileConversionComponent, TriggerEvent>(OnTrigger);
        SubscribeLocalEvent<TileConversionComponent, MapInitEvent>(OnMapInit);
    }

    private void OnTrigger(Entity<TileConversionComponent> ent, ref TriggerEvent args)
    {
        ent.Comp.ConversionMaxTicks++;
        ent.Comp.Enabled = true;
        args.Handled = true;
    }

    //when the entity spawns, add all neighbouring tiles to the convertable list
    private void OnMapInit(Entity<TileConversionComponent> ent, ref MapInitEvent args)
    {
        RecalculateStartingTiles(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var blanktimer = EntityQueryEnumerator<TileConversionComponent>();
        while (blanktimer.MoveNext(out var uid, out var comp))
        {
            if (comp.Enabled && _timing.CurTime >= comp.ConversionTimer)
            {
                comp.ConversionTimer = _timing.CurTime + comp.ConversionTime;
                ConvertTiles((uid, comp));
                if (comp.ConversionTicks <= comp.ConversionMaxTicks)
                {
                    comp.ConversionTicks++;
                    comp.ConversionChance -= comp.ChanceReduction;
                }

                if (comp.ConversionTicks >= comp.ConversionMaxTicks && comp.AutoDisable)
                    comp.Enabled =
                        false; //maybe just remComp this? atm nothing re-enables a converter so that should be safe to do?
            }
        }
    }

    private void ConvertTiles(Entity<TileConversionComponent> ent)
    {
        var xform = Transform(ent);
        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var mapGrid))
            return;

        var convertTile = (ContentTileDefinition)_tileDefinition[_rand.Pick(ent.Comp.ConversionTiles)];

        //if this is a mobile converter, reset the list of convertable tiles every attempt.
        //not a super clean solution because I didn't account for the astral nova in the first rewrite but it works well enough for our purposes.
        if (ent.Comp.Mobile)
            RecalculateStartingTiles(ent);

        //go over every convertable tile
        foreach (var pos in
                 new HashSet<Vector2i>(ent.Comp.ConvertableTiles)) //we love avoiding ConcurrentModificationExceptions
        {
            var tileRef = _map.GetTileRef((gridUid, mapGrid), pos);
            if (tileRef.Tile.TypeId == convertTile.TileId ||
                tileRef.Tile.IsEmpty) //if it's already converted (or space), remove it from the list and continue
            {
                ent.Comp.ConvertableTiles.Remove(pos);
                continue;
            }

            if (_rand.Prob(ent.Comp.ConversionChance)) //if it rolls good
            {
                //replace & variantise the tile
                _tile.ReplaceTile(tileRef, convertTile);
                _tile.PickVariant(convertTile);

                //then add the new neighbours as targets as long as they're not already converted
                foreach (var neighbourPos in _neighbourPositions)
                {
                    var neighbourRef = _map.GetTileRef((gridUid, mapGrid), tileRef.GridIndices + neighbourPos);
                    if (neighbourRef.Tile.TypeId == convertTile.TileId ||
                        tileRef.Tile.IsEmpty) //ignore already converted (or space) tiles
                        continue;

                    ent.Comp.ConvertableTiles.Add(neighbourRef.GridIndices);
                }

                //convert anything that can be converted
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
                if (ent.Comp.UseVfx)
                    Spawn(ent.Comp.TileConvertVfx, _turfs.GetTileCenter(tileRef));

                ent.Comp.ConvertableTiles.Remove(pos);
            }
        }
    }

    #region API
    public void SetConversionTime(Entity<TileConversionComponent> ent, TimeSpan time)
    {
        ent.Comp.ConversionTime = time;
    }

    public void Enable(Entity<TileConversionComponent> ent, bool recalculate = true)
    {
        ent.Comp.Enabled = true;

        if (recalculate)
            RecalculateStartingTiles(ent);
    }

    public void RecalculateStartingTiles(Entity<TileConversionComponent> ent)
    {
        ent.Comp.ConvertableTiles.Clear();

        var xform = Transform(ent);

        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var mapGrid))
            return;

        var grid = (gridUid, mapGrid);
        var tile = _map.GetTileRef(grid, xform.Coordinates);

        if (ent.Comp.FloodFillStarting) //todo make this async? it doesn't actually run that much though
        {
            var convertTile = (ContentTileDefinition)_tileDefinition[_rand.Pick(ent.Comp.ConversionTiles)];
            var visitedTiles = new HashSet<Vector2i>();
            var tilesToVisit = new HashSet<Vector2i> { tile.GridIndices };

            var count = 0;

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
                        //else, it's not been converted, so mark it as visited and add it to the convertable tiles list
                        //we don't care if the tile is empty, that'll get checked later
                        visitedTiles.Add(neighbourRef.GridIndices);
                        ent.Comp.ConvertableTiles.Add(neighbourRef.GridIndices);
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
            //add every neighbouring tile to the convertable list
            //don't bother checking eligibility at this point because it'll get done later anyway
            foreach (var neighbourPos in _neighbourPositions)
            {
                var neighbourRef = _map.GetTileRef((gridUid, mapGrid), tile.GridIndices + neighbourPos);

                ent.Comp.ConvertableTiles.Add(neighbourRef.GridIndices);
            }
        }
    }

    #endregion
}
