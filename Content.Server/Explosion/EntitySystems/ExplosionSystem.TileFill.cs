using System.Linq;
using System.Numerics;
using Content.Shared.Administration;
using Content.Shared.Explosion;
using Content.Shared.Explosion.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Server.Explosion.EntitySystems;

// This partial part of the explosion system has all of the functions used to create the actual explosion map.
// I.e, to get the sets of tiles & intensity values that describe an explosion.

public sealed partial class ExplosionSystem : EntitySystem
{
    /// <summary>
    ///     This is the main explosion generating function.
    /// </summary>
    /// <param name="epicenter">The center of the explosion</param>
    /// <param name="typeID">The explosion type. this determines the explosion damage</param>
    /// <param name="totalIntensity">The final sum of the tile intensities. This governs the overall size of the
    /// explosion</param>
    /// <param name="slope">How quickly does the intensity decrease when moving away from the epicenter.</param>
    /// <param name="maxIntensity">The maximum intensity that the explosion can have at any given tile. This
    /// effectively caps the damage that this explosion can do.</param>
    /// <returns>A list of tile-sets and a list of intensity values which describe the explosion.</returns>
    private (int, List<float>, ExplosionSpaceTileFlood?, Dictionary<EntityUid, ExplosionGridTileFlood>, Matrix3)? GetExplosionTiles(
        MapCoordinates epicenter,
        string typeID,
        float totalIntensity,
        float slope,
        float maxIntensity)
    {
        if (totalIntensity <= 0 || slope <= 0)
            return null;

        if (!_explosionTypes.TryGetValue(typeID, out var typeIndex))
        {
            Log.Error("Attempted to spawn explosion using a prototype that was not defined during initialization. Explosion prototype hot-reload is not currently supported.");
            return null;
        }

        Vector2i initialTile;
        EntityUid? epicentreGrid = null;
        var (localGrids, referenceGrid, maxDistance) = GetLocalGrids(epicenter, totalIntensity, slope, maxIntensity);

        // get the epicenter tile indices
        if (_mapManager.TryFindGridAt(epicenter, out var gridUid, out var candidateGrid) &&
            candidateGrid.TryGetTileRef(candidateGrid.WorldToTile(epicenter.Position), out var tileRef) &&
            !tileRef.Tile.IsEmpty)
        {
            epicentreGrid = gridUid;
            initialTile = tileRef.GridIndices;
        }
        else if (referenceGrid != null)
        {
            // reference grid defines coordinate system that the explosion in space will use
            initialTile = _mapManager.GetGrid(referenceGrid.Value).WorldToTile(epicenter.Position);
        }
        else
        {
            // this is a space-based explosion that (should) not touch any grids.
            initialTile = new Vector2i(
                    (int) Math.Floor(epicenter.Position.X / DefaultTileSize),
                    (int) Math.Floor(epicenter.Position.Y / DefaultTileSize));
        }

        // Main data for the exploding tiles in space and on various grids
        Dictionary<EntityUid, ExplosionGridTileFlood> gridData = new();
        ExplosionSpaceTileFlood? spaceData = null;

        // The intensity slope is how much the intensity drop over a one-tile distance. The actual algorithm step-size is half of thhat.
        var stepSize = slope / 2;

        // Hashsets used for when grid-based explosion propagate into space. Basically: used to move data between
        // `gridData` and `spaceData` in-between neighbor finding iterations.
        HashSet<Vector2i> spaceJump = new();
        HashSet<Vector2i> previousSpaceJump;

        // As above, but for space-based explosion propagating from space onto grids.
        HashSet<EntityUid> encounteredGrids = new();
        Dictionary<EntityUid, HashSet<Vector2i>>? previousGridJump;

        // variables for transforming between grid and space-coordinates
        var spaceMatrix = Matrix3.Identity;
        var spaceAngle = Angle.Zero;
        if (referenceGrid != null)
        {
            var xform = Transform(_mapManager.GetGrid(referenceGrid.Value).Owner);
            spaceMatrix = xform.WorldMatrix;
            spaceAngle = xform.WorldRotation;
        }

        // is the explosion starting on a grid?
        if (epicentreGrid != null)
        {
            // set up the initial `gridData` instance
            encounteredGrids.Add(epicentreGrid.Value);

            if (!_airtightMap.TryGetValue(epicentreGrid.Value, out var airtightMap))
                airtightMap = new();

            var initialGridData = new ExplosionGridTileFlood(
                _mapManager.GetGrid(epicentreGrid.Value),
                airtightMap,
                maxIntensity,
                stepSize,
                typeIndex,
                _gridEdges[epicentreGrid.Value],
                referenceGrid,
                spaceMatrix,
                spaceAngle);

            gridData[epicentreGrid.Value] = initialGridData;

            initialGridData.InitTile(initialTile);
        }
        else
        {
            // set up the space explosion data
            spaceData = new ExplosionSpaceTileFlood(this, epicenter, referenceGrid, localGrids, maxDistance);
            spaceData.InitTile(initialTile);
        }

        // Is this even a multi-tile explosion?
        if (totalIntensity < stepSize)
            // Bit anticlimactic. All that set up for nothing....
            return (1, new List<float> { totalIntensity }, spaceData, gridData, spaceMatrix);

        // These variables keep track of the total intensity we have distributed
        List<int> tilesInIteration = new() { 1 };
        List<float> iterationIntensity = new() {stepSize};
        var totalTiles = 1;
        var remainingIntensity = totalIntensity - stepSize;

        var iteration = 1;
        var maxIntensityIndex = 0;

        // If an explosion is trapped in an indestructible room, we can end the neighbor finding steps early.
        // These variables are used to check if we can abort early.
        float previousIntensity;
        var intensityUnchangedLastLoop = false;

        // Main flood-fill / neighbor-finding loop
        while (remainingIntensity > 0 && iteration <= MaxIterations && totalTiles < MaxArea)
        {
            previousIntensity = remainingIntensity;

            // First, we increase the intensity of the tiles that were already discovered in previous iterations.
            for (var i = maxIntensityIndex; i < iteration; i++)
            {
                var intensityIncrease = MathF.Min(stepSize, maxIntensity - iterationIntensity[i]);

                if (tilesInIteration[i] * intensityIncrease >= remainingIntensity)
                {
                    // there is not enough intensity left to distribute. add a fractional amount and break.
                    iterationIntensity[i] += remainingIntensity / tilesInIteration[i];
                    remainingIntensity = 0;
                    break;
                }

                iterationIntensity[i] += intensityIncrease;
                remainingIntensity -= tilesInIteration[i] * intensityIncrease;

                // Has this tile-set has reached max intensity? If so, stop iterating over it in  future
                if (intensityIncrease < stepSize)
                    maxIntensityIndex++;
            }

            if (remainingIntensity <= 0) break;

            // Next, we will add a new iteration of tiles

            // In order to treat "cost" of moving off a grid on the same level as moving onto a grid, both space -> grid and grid -> space have to be delayed by one iteration.
            previousSpaceJump = spaceJump;
            previousGridJump = spaceData?.GridJump;
            spaceJump = new();

            var newTileCount = 0;

            if (previousGridJump != null)
                encounteredGrids.UnionWith(previousGridJump.Keys);

            foreach (var grid in encounteredGrids)
            {
                // is this a new grid, for which we must create a new explosion data set
                if (!gridData.TryGetValue(grid, out var data))
                {
                    if (!_airtightMap.TryGetValue(grid, out var airtightMap))
                        airtightMap = new();

                    data = new ExplosionGridTileFlood(
                        _mapManager.GetGrid(grid),
                        airtightMap,
                        maxIntensity,
                        stepSize,
                        typeIndex,
                        _gridEdges[grid],
                        referenceGrid,
                        spaceMatrix,
                        spaceAngle);

                    gridData[grid] = data;
                }

                // get the new neighbours, and populate gridToSpaceTiles in the process.
                newTileCount += data.AddNewTiles(iteration, previousGridJump?.GetValueOrDefault(grid));
                spaceJump.UnionWith(data.SpaceJump);
            }

            // if space-data is null, but some grid-based explosion reached space, we need to initialize it.
            if (spaceData == null && previousSpaceJump.Count != 0)
                spaceData = new ExplosionSpaceTileFlood(this, epicenter, referenceGrid, localGrids, maxDistance);

            // If the explosion has reached space, do that neighbors finding step as well.
            if (spaceData != null)
                newTileCount += spaceData.AddNewTiles(iteration, previousSpaceJump);

            // Does adding these tiles bring us above the total target intensity?
            tilesInIteration.Add(newTileCount);
            if (newTileCount * stepSize >= remainingIntensity)
            {
                iterationIntensity.Add(remainingIntensity / newTileCount);
                break;
            }

            // add the new tiles and decrement available intensity
            remainingIntensity -= newTileCount * stepSize;
            iterationIntensity.Add(stepSize);
            totalTiles += newTileCount;

            // It is possible that the explosion has some max intensity and is stuck in a container whose walls it
            // cannot break. if the remaining intensity remains unchanged TWO loops in a row, we know that this is the
            // case.
            if (intensityUnchangedLastLoop && remainingIntensity == previousIntensity)
                break;

            intensityUnchangedLastLoop = remainingIntensity == previousIntensity;
            iteration += 1;
        }

        // Neighbor finding is done. Perform final clean up and return.
        foreach (var grid in gridData.Values)
        {
            grid.CleanUp();
        }
        spaceData?.CleanUp();

        return (totalTiles, iterationIntensity, spaceData, gridData, spaceMatrix);
    }

    /// <summary>
    ///     Look for grids in an area and returns them. Also selects a special grid that will be used to determine the
    ///     orientation of an explosion in space.
    /// </summary>
    /// <remarks>
    ///     Note that even though an explosion may start ON a grid, the explosion in space may still be orientated to
    ///     match a separate grid. This is done so that if you have something like a tiny suicide-bomb shuttle exploding
    ///     near a large station, the explosion will still orient to match the station, not the tiny shuttle.
    /// </remarks>
    public (List<EntityUid>, EntityUid?, float) GetLocalGrids(MapCoordinates epicenter, float totalIntensity, float slope, float maxIntensity)
    {
        // Get the explosion radius (approx radius if it were in open-space). Note that if the explosion is confined in
        // some directions but not in others, the actual explosion may reach further than this distance from the
        // epicenter. Conversely, it might go nowhere near as far.
        var radius = 0.5f + IntensityToRadius(totalIntensity, slope, maxIntensity);

        // to avoid a silly lookup for silly input numbers, cap the radius to half of the theoretical maximum (lookup area gets doubled later on).
        radius = Math.Min(radius, MaxIterations / 4);

        EntityUid? referenceGrid = null;
        float mass = 0;

        // First attempt to find a grid that is relatively close to the explosion's center. Instead of looking in a
        // diameter x diameter sized box, use a smaller box with radius sized sides:
        var box = Box2.CenteredAround(epicenter.Position, new Vector2(radius, radius));

        foreach (var grid in _mapManager.FindGridsIntersecting(epicenter.MapId, box))
        {
            if (TryComp(grid.Owner, out PhysicsComponent? physics) && physics.Mass > mass)
            {
                mass = physics.Mass;
                referenceGrid = grid.Owner;
            }
        }

        // Next, we use a much larger lookup to determine all grids relevant to the explosion. This is used to determine
        // what grids should be included during the grid-edge transformation steps. This means that if a grid is not in
        // this set, the explosion can never propagate from space onto this grid.

        // As mentioned before, the `diameter` is only indicative, as an explosion that is obstructed (e.g., in a
        // tunnel) may travel further away from the epicenter. But this should be very rare for space-traversing
        // explosions. So instead of using the largest possible distance that an explosion could theoretically travel
        // and using that for the grid look-up, we will just arbitrarily fudge the lookup size to be twice the diameter.

        radius *= 4;
        box = Box2.CenteredAround(epicenter.Position, new Vector2(radius, radius));
        var mapGrids = _mapManager.FindGridsIntersecting(epicenter.MapId, box).ToList();
        var grids = mapGrids.Select(x => x.Owner).ToList();

        if (referenceGrid != null)
            return (grids, referenceGrid, radius);

        // We still don't have are reference grid. So lets also look in the enlarged region
        foreach (var grid in mapGrids)
        {
            if (TryComp(grid.Owner, out PhysicsComponent? physics) && physics.Mass > mass)
            {
                mass = physics.Mass;
                referenceGrid = grid.Owner;
            }
        }

        return (grids, referenceGrid, radius);
    }

    public ExplosionVisualsState? GenerateExplosionPreview(SpawnExplosionEuiMsg.PreviewRequest request)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var results = GetExplosionTiles(
            request.Epicenter,
            request.TypeId,
            request.TotalIntensity,
            request.IntensitySlope,
            request.MaxIntensity);

        if (results == null)
            return null;

        var (area, iterationIntensity, spaceData, gridData, spaceMatrix) = results.Value;

        Log.Info($"Generated explosion preview with {area} tiles in {stopwatch.Elapsed.TotalMilliseconds}ms");

        Dictionary<NetEntity, Dictionary<int, List<Vector2i>>> tileLists = new();
        foreach (var (grid, data) in gridData)
        {
            tileLists.Add(GetNetEntity(grid), data.TileLists);
        }

        return new ExplosionVisualsState(
            request.Epicenter,
            request.TypeId,
            iterationIntensity,
            spaceData?.TileLists,
            tileLists, spaceMatrix,
            spaceData?.TileSize ?? DefaultTileSize
            );
    }
}
