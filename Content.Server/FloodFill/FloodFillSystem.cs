using System.Linq;
using Content.Server.FloodFill.TileFloods;
using Content.Shared.Atmos;
using Robust.Shared.Map;

namespace Content.Server.FloodFill;

// ReSharper disable CompareOfFloatsByEqualityOperator
public sealed partial class FloodFillSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;

    /// <summary>
    ///     "Tile-size" for space when there are no nearby grids to use as a reference.
    /// </summary>
    public const ushort DefaultTileSize = 1;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GridStartupEvent>(OnGridStartup);
        SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoved);
        SubscribeLocalEvent<TileChangedEvent>(OnTileChanged);
    }

    /// <summary>
    ///     Start fill flood algorithm from selected position.
    /// </summary>
    /// <param name="epicenter">The center of the fill flood algorithm.</param>
    /// <param name="totalIntensity">The total "energy" of tile flood. This governs the overall size of the
    /// tile flood.</param>
    /// <param name="slope">How quickly does the intensity decrease when moving away from the epicenter.</param>
    /// <param name="maxIntensity">The maximum intensity that the flood can have at any given tile.</param>
    /// <param name="resistanceMap">The map of resistance to incoming flood intensity. First key is grid uid,
    /// second is position of resistance tile on a grid.</param>
    /// <param name="toleranceIndex">Index for tolerance array in <see cref="TileData"/> in resistance map.</param>
    /// <param name="maxIterations">Max amount of iterations that fill flood allowed to do.</param>
    /// <param name="maxArea">Max amount of tiles that </param>
    public FloodFillResult? DoFloodTile(
            MapCoordinates epicenter,
            float totalIntensity,
            float slope,
            float maxIntensity,
            Dictionary<EntityUid, Dictionary<Vector2i, TileData>> resistanceMap,
            int toleranceIndex,
            int maxIterations,
            int maxArea)
    {
        if (totalIntensity <= 0 || slope <= 0)
            return null;

        Vector2i initialTile;
        EntityUid? epicentreGrid = null;
        var (localGrids, referenceGrid, maxDistance) = GetLocalGrids(epicenter, totalIntensity, slope, maxIntensity, maxIterations);

        if (_mapManager.TryFindGridAt(epicenter, out var candidateGrid) &&
            candidateGrid.TryGetTileRef(candidateGrid.WorldToTile(epicenter.Position), out var tileRef) &&
            !tileRef.Tile.IsEmpty)
        {
            epicentreGrid = candidateGrid.GridEntityId;
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
        Dictionary<EntityUid, GridTileFlood> gridData = new();
        SpaceTileFlood? spaceData = null;

        // The intensity slope is how much the intensity drop over a one-tile distance. The actual algorithm step-size is half of that.
        var stepSize = slope / 2;

        // Hashsets used for when grid-based explosion propagate into space. Basically: used to move data between
        // `gridData` and `spaceData` in-between neighbor finding iterations.
        HashSet<Vector2i> spaceJump = new();
        HashSet<Vector2i> previousSpaceJump;

        // As above, but for space-based explosion propagating from space onto grids.
        HashSet<EntityUid> encounteredGrids = new();
        Dictionary<EntityUid, HashSet<Vector2i>>? previousGridJump;

        // variables for transforming between grid and space-coordiantes
        var spaceMatrix = Matrix3.Identity;
        var spaceAngle = Angle.Zero;
        if (referenceGrid != null)
        {
            var xform = Transform(_mapManager.GetGrid(referenceGrid.Value).GridEntityId);
            spaceMatrix = xform.WorldMatrix;
            spaceAngle = xform.WorldRotation;
        }

        // is the explosion starting on a grid?
        if (epicentreGrid != null)
        {
            // set up the initial `gridData` instance
            encounteredGrids.Add(epicentreGrid.Value);

            if (!resistanceMap.TryGetValue(epicentreGrid.Value, out var airtightMap))
                airtightMap = new();

            var initialGridData = new GridTileFlood(
                _mapManager.GetGrid(epicentreGrid.Value),
                airtightMap,
                maxIntensity,
                stepSize,
                toleranceIndex,
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
            spaceData = new SpaceTileFlood(this, epicenter, referenceGrid, localGrids, maxDistance);
            spaceData.InitTile(initialTile);
        }

        // Is this even a multi-tile explosion?
        if (totalIntensity < stepSize)
        {
            // Bit anticlimactic. All that set up for nothing....
            return new FloodFillResult(1, new List<float> { totalIntensity },
                spaceData, gridData, spaceMatrix);
        }


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
        while (remainingIntensity > 0 && iteration <= maxIterations && totalTiles < maxArea)
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

            if (remainingIntensity <= 0)
                break;

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
                    if (!resistanceMap.TryGetValue(grid, out var airtightMap))
                        airtightMap = new();

                    data = new GridTileFlood(
                        _mapManager.GetGrid(grid),
                        airtightMap,
                        maxIntensity,
                        stepSize,
                        toleranceIndex,
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
                spaceData = new SpaceTileFlood(this, epicenter, referenceGrid, localGrids, maxDistance);

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

        return new FloodFillResult(totalTiles, iterationIntensity, spaceData, gridData, spaceMatrix);
    }

    public (List<EntityUid>, EntityUid?, float) GetLocalGrids(MapCoordinates epicenter, float totalIntensity,
        float slope, float maxIntensity, int maxIterations)
    {
        // Get the explosion radius (approx radius if it were in open-space). Note that if the explosion is confined in
        // some directions but not in others, the actual explosion may reach further than this distance from the
        // epicenter. Conversely, it might go nowhere near as far.
        var radius = 0.5f + IntensityToRadius(totalIntensity, slope, maxIntensity);

        // to avoid a silly lookup for silly input numbers, cap the radius to half of the theoretical maximum (lookup area gets doubled later on).
        // ReSharper disable once PossibleLossOfFraction
        radius = Math.Min(radius, maxIterations / 4);

        EntityUid? referenceGrid = null;
        float mass = 0;

        // First attempt to find a grid that is relatively close to the explosion's center. Instead of looking in a
        // diameter x diameter sized box, use a smaller box with radius sized sides:
        var box = Box2.CenteredAround(epicenter.Position, (radius, radius));

        foreach (var grid in _mapManager.FindGridsIntersecting(epicenter.MapId, box))
        {
            if (TryComp(grid.GridEntityId, out PhysicsComponent? physics) && physics.Mass > mass)
            {
                mass = physics.Mass;
                referenceGrid = grid.GridEntityId;
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
        box = Box2.CenteredAround(epicenter.Position, (radius, radius));
        var mapGrids = _mapManager.FindGridsIntersecting(epicenter.MapId, box).ToList();
        var grids = mapGrids.Select(x => x.GridEntityId).ToList();

        if (referenceGrid != null)
            return (grids, referenceGrid, radius);

        // We still don't have are reference grid. So lets also look in the enlarged region
        foreach (var grid in mapGrids)
        {
            if (TryComp(grid.GridEntityId, out PhysicsComponent? physics) && physics.Mass > mass)
            {
                mass = physics.Mass;
                referenceGrid = grid.GridEntityId;
            }
        }

        return (grids, referenceGrid, radius);
    }

    /// <summary>
    ///     Find the strength needed to generate an explosion of a given radius. More useful for radii larger then 4, when the explosion becomes less "blocky".
    /// </summary>
    /// <remarks>
    ///     This assumes the explosion is in a vacuum / unobstructed. Given that explosions are not perfectly
    ///     circular, here radius actually means the sqrt(Area/pi), where the area is the total number of tiles
    ///     covered by the explosion. Until you get to radius 30+, this is functionally equivalent to the
    ///     actual radius.
    /// </remarks>
    public float RadiusToIntensity(float radius, float slope, float maxIntensity = 0)
    {
        // If you consider the intensity at each tile in an explosion to be a height. Then a circular explosion is
        // shaped like a cone. So total intensity is like the volume of a cone with height = slope * radius. Of
        // course, as the explosions are not perfectly circular, this formula isn't perfect, but the formula works
        // reasonably well.

        // This should actually use the formula for the volume of a distorted octagonal frustum. But this is good
        // enough.

        var coneVolume = slope * MathF.PI / 3 * MathF.Pow(radius, 3);

        if (maxIntensity <= 0 || slope * radius < maxIntensity)
            return coneVolume;

        // This explosion is limited by the maxIntensity.
        // Instead of a cone, we have a conical frustum.

        // Subtract the volume of the missing cone segment, with height:
        var h = slope * radius - maxIntensity;
        return coneVolume - h * MathF.PI / 3 * MathF.Pow(h / slope, 2);
    }

    /// <summary>
    ///     Inverse formula for <see cref="RadiusToIntensity"/>
    /// </summary>
    public float IntensityToRadius(float totalIntensity, float slope, float maxIntensity)
    {
        // max radius to avoid being capped by max-intensity
        var r0 = maxIntensity / slope;

        // volume at r0
        var v0 = RadiusToIntensity(r0, slope);

        if (totalIntensity <= v0)
        {
            // maxIntensity is a non-issue, can use simple inverse formula
            return MathF.Cbrt(3 * totalIntensity / (slope * MathF.PI));
        }

        return r0 * (MathF.Sqrt(12 * totalIntensity/ v0 - 3) / 6 + 0.5f);
    }
}

/// <summary>
///     Data struct that describes the entities blocking fill flood on a tile.
/// </summary>
public struct TileData
{
    /// <summary>
    ///     Resistance towards further fill flood propagation.
    ///     You can save different type of tolerance into different indexes of array.
    /// </summary>
    /// <example>
    ///     In explosion system each tolerance element is mapped
    ///     into resistance to different types of explosion.
    /// </example>
    public readonly float[] Tolerance;

    /// <summary>
    ///     From what incoming direction of fill flood will be blocked by tolerance.
    /// </summary>
    public readonly AtmosDirection BlockedDirections = AtmosDirection.Invalid;

    public TileData(float[] tolerance, AtmosDirection blockedDirections)
    {
        Tolerance = tolerance;
        BlockedDirections = blockedDirections;
    }
}
