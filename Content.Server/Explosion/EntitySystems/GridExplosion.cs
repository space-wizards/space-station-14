using Content.Shared.Atmos;
using Robust.Shared.Map;

namespace Content.Server.Explosion.EntitySystems;

internal sealed class GridExplosion : TileExplosion
{
    public IMapGrid Grid;
    private bool _needToTransform = false;

    private Matrix3 _matrix = Matrix3.Identity;
    private Vector2 _offset;

    private HashSet<Vector2i> _processedSpaceTiles = new();

    // Tiles which neighbor an exploding tile, but have not yet had the explosion spread to them due to an
    // airtight entity on the exploding tile that prevents the explosion from spreading in that direction. These
    // will be added as a neighbor after some delay, once the explosion on that tile is sufficiently strong to
    // destroy the airtight entity.
    private Dictionary<int, List<(Vector2i, AtmosDirection)>> _delayedNeighbors = new();

    private Dictionary<Vector2i, TileData> _airtightMap;

    private float _maxIntensity;
    private float _intensityStepSize;
    private string _typeID;

    /// <summary>
    ///     Tiles on this grid that are not actually on this grid.... uhh ... yeah.... look its faster than checking
    ///     atmos directions every iteration.
    /// </summary>
    private HashSet<Vector2i> _spaceTiles = new();

    public HashSet<Vector2i> SpaceJump = new();

    private Dictionary<Vector2i, AtmosDirection> _edgeTiles;

    public GridExplosion(
        IMapGrid grid,
        Dictionary<Vector2i, TileData> airtightMap,
        float maxIntensity,
        float intensityStepSize,
        string typeID,
        Dictionary<Vector2i, AtmosDirection> edgeTiles,
        GridId? referenceGrid,
        Matrix3 spaceMatrix,
        Angle spaceAngle)
    {
        Grid = grid;
        _airtightMap = airtightMap;
        _maxIntensity = maxIntensity;
        _intensityStepSize = intensityStepSize;
        _typeID = typeID;
        _edgeTiles = edgeTiles;

        // initialise SpaceTiles
        foreach (var (tile, dir) in _edgeTiles)
        {
            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var direction = (AtmosDirection) (1 << i);
                if (dir.IsFlagSet(direction))
                    _spaceTiles.Add(tile.Offset(direction));
            }
        }

        // TODO EXPLOSIONS fix this shit.
        foreach (var tile in _edgeTiles.Keys)
        {
            foreach (var diagTile in ExplosionSystem.GetDiagonalNeighbors(tile))
            {
                if (_spaceTiles.Contains(diagTile))
                    continue;

                if (!Grid.TryGetTileRef(diagTile, out var tileRef) || tileRef.Tile.IsEmpty)
                    _spaceTiles.Add(diagTile);
            }
        }

        if (referenceGrid == Grid.Index)
            return;

        _needToTransform = true;
        var transform = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Grid.GridEntityId);
        var size = (float) Grid.TileSize;

        _matrix.R0C2 = size / 2;
        _matrix.R1C2 = size / 2;
        _matrix *= transform.WorldMatrix * Matrix3.Invert(spaceMatrix);
        var relativeAngle = transform.WorldRotation - spaceAngle;
        _offset = relativeAngle.RotateVec((size / 4, size / 4));
    }

    public int AddNewTiles(int iteration, HashSet<Vector2i>? gridJump)
    {
        SpaceJump = new();
        NewTiles = new();
        NewBlockedTiles = new();

        // Mark tiles as entered if any were just freed due to airtight/explosion blockers being destroyed.
        if (FreedTileLists.TryGetValue(iteration, out var freed))
        {
            freed.ExceptWith(EnteredBlockedTiles);
            EnteredBlockedTiles.UnionWith(freed);
            NewFreedTiles = freed;
        }
        else
        {
            NewFreedTiles = new();
            FreedTileLists[iteration] = NewFreedTiles;
        }

        // Add adjacent tiles
        if (TileLists.TryGetValue(iteration - 2, out var adjacent))
            AddNewAdjacentTiles(iteration, adjacent, false);
        if (FreedTileLists.TryGetValue(iteration - 2, out var delayedAdjacent))
            AddNewAdjacentTiles(iteration, delayedAdjacent, true);

        // Add diagonal tiles
        if (TileLists.TryGetValue(iteration - 3, out var diagonal))
            AddNewDiagonalTiles(iteration, diagonal, false);
        if (FreedTileLists.TryGetValue(iteration - 3, out var delayedDiagonal))
            AddNewDiagonalTiles(iteration, delayedDiagonal, true);

        // Add delayed tiles
        AddDelayedNeighbors(iteration);

        // Tiles from Spaaaace
        if (gridJump != null)
        {
            foreach (var tile in gridJump)
            {
                ProcessNewTile(iteration, tile, AtmosDirection.Invalid);
            }
        }

        // Store new tiles
        if (NewTiles.Count != 0)
            TileLists[iteration] = NewTiles;
        if (NewBlockedTiles.Count != 0)
            BlockedTileLists[iteration] = NewBlockedTiles;

        return NewTiles.Count + NewBlockedTiles.Count;
    }

    protected override void ProcessNewTile(int iteration, Vector2i tile, AtmosDirection entryDirections)
    {
        // Is there an airtight blocker on this tile?
        if (!_airtightMap.TryGetValue(tile, out var tileData))
        {
            // No blocker. Ezy. Though maybe this a space tile?

            if (_spaceTiles.Contains(tile))
                JumpToSpace(tile);
            else if (ProcessedTiles.Add(tile))
                NewTiles.Add(tile);

            return;
        }

        // If the explosion is entering this new tile from an unblocked direction, we add it directly. Note that because
        // for space -> grid jumps, we don't have a direction from which the explosion came, we will only assume it is
        // unblocked if all space-facing directions are unblocked. Though this could eventually be done properly.

        bool blocked;
        var blockedDirections = tileData.BlockedDirections;
        if (entryDirections == AtmosDirection.Invalid) // is coming from space?
        {
            var spaceDirections = _edgeTiles[tile];
            blocked = (blockedDirections & spaceDirections) != 0; // at least one space direction is blocked.
        }
        else
            blocked = (blockedDirections & entryDirections) == entryDirections;// **ALL** entry directions are blocked

        if (blocked)
        {
            // was this tile already entered from some other direction?
            if (EnteredBlockedTiles.Contains(tile))
                return;

            // Did the explosion already attempt to enter this tile from some other direction? 
            if (!UnenteredBlockedTiles.Add(tile))
                return;

            NewBlockedTiles.Add(tile);

            // At what explosion iteration would this blocker be destroyed?

            if (!tileData.ExplosionTolerance.TryGetValue(_typeID, out var sealIntegrity))
                sealIntegrity = float.MaxValue; // indestructible airtight entity

            var clearIteration = iteration + (int) MathF.Ceiling(sealIntegrity / _intensityStepSize);
            if (FreedTileLists.TryGetValue(clearIteration, out var list))
                list.Add(tile);
            else
                FreedTileLists[clearIteration] = new() { tile };

            return;
        }

        // was this tile already entered from some other direction?
        if (!EnteredBlockedTiles.Add(tile))
            return;

        // Did the explosion already attempt to enter this tile from some other direction? 
        if (UnenteredBlockedTiles.Contains(tile))
        {
            NewFreedTiles.Add(tile);
            return;
        }

        // This is a completely new tile, and we just so happened to enter it from an unblocked direction.
        NewTiles.Add(tile);
    }

    private void JumpToSpace(Vector2i tile)
    {
        // Did we already jump/process this tile?
        if (!_processedSpaceTiles.Add(tile))
            return;

        if (!_needToTransform)
        {
            SpaceJump.Add(tile);
            return;
        }

        var center = _matrix.Transform(tile);
        SpaceJump.Add(new((int) MathF.Floor(center.X + _offset.X), (int) MathF.Floor(center.Y + _offset.Y)));
        SpaceJump.Add(new((int) MathF.Floor(center.X - _offset.Y), (int) MathF.Floor(center.Y + _offset.X)));
        SpaceJump.Add(new((int) MathF.Floor(center.X - _offset.X), (int) MathF.Floor(center.Y - _offset.Y)));
        SpaceJump.Add(new((int) MathF.Floor(center.X + _offset.Y), (int) MathF.Floor(center.Y - _offset.X)));
    }

    private void AddDelayedNeighbors(int iteration)
    {
        if (!_delayedNeighbors.TryGetValue(iteration, out var delayed))
            return;

        foreach (var (tile, direction) in delayed)
        {
            ProcessNewTile(iteration, tile, direction);
        }

        _delayedNeighbors.Remove(iteration);
    }

    // Gets the tiles that are directly adjacent to other tiles. If a currently exploding tile has an airtight entity
    // that blocks the explosion from propagating in some direction, those tiles are added to a list of delayed tiles
    // that will be added to the explosion in some future iteration.
    private void AddNewAdjacentTiles(int iteration, IEnumerable<Vector2i> tiles, bool ignoreTileBlockers = false)
    {
        foreach (var tile in tiles)
        {
            var blockedDirections = AtmosDirection.Invalid;
            float sealIntegrity = 0;

            // Note that if (grid, tile) is not a valid key, then airtight.BlockedDirections will default to 0 (no blocked directions)
            if (_airtightMap.TryGetValue(tile, out var tileData))
            {
                blockedDirections = tileData.BlockedDirections;
                if (!tileData.ExplosionTolerance.TryGetValue(_typeID, out sealIntegrity))
                    sealIntegrity = float.MaxValue; // indestructible airtight entity
            }

            // First, yield any neighboring tiles that are not blocked by airtight entities on this tile
            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var direction = (AtmosDirection) (1 << i);
                if (ignoreTileBlockers || !blockedDirections.IsFlagSet(direction))
                {
                    ProcessNewTile(iteration, tile.Offset(direction), direction.GetOpposite());
                }
            }

            // If there are no blocked directions, we are done with this tile.
            if (ignoreTileBlockers || blockedDirections == AtmosDirection.Invalid)
                continue;

            // This tile has one or more airtight entities anchored to it blocking the explosion from traveling in
            // some directions. First, check whether this blocker can even be destroyed by this explosion?
            if (sealIntegrity > _maxIntensity || float.IsNaN(sealIntegrity))
                continue;

            // At what explosion iteration would this blocker be destroyed?
            var clearIteration = iteration + (int) MathF.Ceiling(sealIntegrity / _intensityStepSize);

            // Get the delayed neighbours list
            if (!_delayedNeighbors.TryGetValue(clearIteration, out var list))
            {
                list = new();
                _delayedNeighbors[clearIteration] = list;
            }

            // Check which directions are blocked, and add them to the list.
            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var direction = (AtmosDirection) (1 << i);
                if (blockedDirections.IsFlagSet(direction))
                {
                    list.Add((tile.Offset(direction), direction.GetOpposite()));
                }
            }
        }
    }

    protected override AtmosDirection GetUnblockedDirectionOrAll(Vector2i tile)
    {
        return ~_airtightMap.GetValueOrDefault(tile).BlockedDirections;
    }
}
