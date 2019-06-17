using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects;
using Content.Server.GameObjects.Components.Atmos;
using Content.Server.Interfaces.Atmos;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Server.Atmos
{
    internal class AtmosphereMap : IAtmosphereMap
    {
#pragma warning disable 649
        // ReSharper disable once InconsistentNaming
        [Dependency] private readonly IMapManager MapManager;
#pragma warning restore 649

        private readonly Dictionary<GridId, GridAtmosphereManager> _gridAtmosphereManagers =
            new Dictionary<GridId, GridAtmosphereManager>();

        public IGridAtmosphereManager GetGridAtmosphereManager(GridId grid)
        {
            if (_gridAtmosphereManagers.TryGetValue(grid, out var manager))
                return manager;

            if (!MapManager.GridExists(grid))
                throw new ArgumentException("Cannot get atmosphere of missing grid", nameof(grid));

            manager = new GridAtmosphereManager(MapManager.GetGrid(grid));
            _gridAtmosphereManagers[grid] = manager;
            return manager;
        }

        public IAtmosphere GetAtmosphere(ITransformComponent position) =>
            GetGridAtmosphereManager(position.GridID).GetAtmosphere(position.GridPosition);

        public void Update(float frameTime)
        {
            foreach (var atmos in _gridAtmosphereManagers.Values)
            {
                atmos.Update(frameTime);
            }
        }
    }

    internal class GridAtmosphereManager : IGridAtmosphereManager
    {
        private readonly IMapGrid _grid;
        private Dictionary<MapIndices, Atmosphere> _atmospheres = new Dictionary<MapIndices, Atmosphere>();

        private HashSet<MapIndices> _invalidatedPositions = new HashSet<MapIndices>();

        public GridAtmosphereManager(IMapGrid grid)
        {
            _grid = grid;
        }

        public IAtmosphere GetAtmosphere(GridCoordinates coordinates) => GetAtmosphere(GridToMapCoord(coordinates));

        private Atmosphere GetAtmosphere(MapIndices pos)
        {
            if (_atmospheres.TryGetValue(pos, out var atmosphere))
                return atmosphere;

            // If this is clearly space - such as someone moving round outside the station - just return immediately
            // and don't add anything to the grid map
            if (IsSpace(pos))
                return null;

            // If the block is obstructed:
            // If the air blocker is marked as 'use adjacent atmosphere' then the cell behaves as if
            // you got one of it's adjacent atmospheres. If not, it returns null.
            var obstruction = GetObstructingComponent(pos);
            if (obstruction != null)
                return obstruction.UseAdjacentAtmosphere ? GetAdjacentAtmospheres(pos).FirstOrDefault().Value : null;

            var connected = FindConnectedCells(pos);

            if (connected == null)
            {
                // Since this is on a floor tile, it's fairly likely this is a room with
                // a breach. As such, cache this position for sake of static objects like
                // air vents which call this constantly.
                _atmospheres[pos] = null;
                return null;
            }

            // TODO create default atmosphere
            atmosphere = new Atmosphere();

            foreach (var c in connected)
                _atmospheres[c] = atmosphere;

            return atmosphere;
        }

        public void Invalidate(GridCoordinates coordinates)
        {
            _invalidatedPositions.Add(GridToMapCoord(coordinates));
        }

        private MapIndices GridToMapCoord(GridCoordinates pos) => _grid.SnapGridCellFor(pos, SnapGridOffset.Center);

        private List<MapIndices> FindConnectedCells(MapIndices start)
        {
            var inner = new HashSet<MapIndices>();
            var edge = new HashSet<MapIndices> {start};

            // Basic inner/edge finder
            // The inner list is all the positions we know are empty, and take no further actions.
            // The edge list is all the positions for whom we have not searched their neighbours.
            // Move stuff from edge to inner, while adding adjacent empty locations to edge
            // When edge is empty, we have filled the entire room.

            while (edge.Count > 0)
            {
                var temp = new List<MapIndices>(edge);
                edge.Clear();
                foreach (var pos in temp)
                {
                    inner.Add(pos);

                    if (IsSpace(pos))
                        return null;

                    Check(inner, edge, pos, Direction.North);
                    Check(inner, edge, pos, Direction.South);
                    Check(inner, edge, pos, Direction.East);
                    Check(inner, edge, pos, Direction.West);
                }
            }

            return new List<MapIndices>(inner);
        }

        private void Check(ICollection<MapIndices> inner, ISet<MapIndices> edges, MapIndices pos, Direction dir)
        {
            pos += (MapIndices) CardinalToIntVec(dir);
            if (IsObstructed(pos))
                return;

            if (inner.Contains(pos))
                return;

            edges.Add(pos);
        }

        private static Vector2i CardinalToIntVec(Direction dir)
        {
            switch (dir)
            {
                case Direction.North:
                    return new Vector2i(0, 1);
                case Direction.East:
                    return new Vector2i(1, 0);
                case Direction.South:
                    return new Vector2i(0, -1);
                case Direction.West:
                    return new Vector2i(-1, 0);
                default:
                    throw new ArgumentException($"Direction dir {dir} is not a cardinal direction", nameof(dir));
            }
        }

        private bool IsObstructed(MapIndices pos) => GetObstructingComponent(pos) != null;

        private AirtightComponent GetObstructingComponent(MapIndices pos)
        {
            foreach (var v in _grid.GetSnapGridCell(pos, SnapGridOffset.Center))
            {
                if (v.Owner.TryGetComponent<AirtightComponent>(out var ac))
                    return ac;
            }

            return null;
        }

        private bool IsSpace(MapIndices pos)
        {
            // TODO use ContentTileDefinition to define in YAML whether or not a tile is considered space
            return _grid.GetTileRef(pos).Tile.IsEmpty;
        }

        public void Update(float frameTime)
        {
            if (_invalidatedPositions.Count != 0)
                Revalidate();
        }

        private void Revalidate()
        {
            foreach (var pos in _invalidatedPositions)
            {
                if (IsObstructed(pos))
                    SplitAtmospheres(pos);
                else
                    MergeAtmospheres(pos);
            }

            _invalidatedPositions.Clear();
        }

        private void SplitAtmospheres(MapIndices pos)
        {
            // Remove the now-covered atmosphere
            _atmospheres.Remove(pos);

            var adjacent = GetAdjacentAtmospheres(pos);
            var adjacentPositions = adjacent.Select(p => Offset(pos, p.Key)).ToList();
            var adjacentAtmospheres = new HashSet<Atmosphere>(adjacent.Values);

            foreach (var atmos in adjacentAtmospheres)
            {
                var i = 0;
                int? spaceIdx = null;
                var sides = new Dictionary<MapIndices, int>();

                foreach (var pair in adjacent.Where(p => p.Value == atmos))
                {
                    var edgePos = Offset(pos, pair.Key);
                    if (sides.ContainsKey(edgePos))
                        continue;

                    var connected = FindConnectedCells(edgePos);
                    if (connected == null)
                    {
                        if (spaceIdx == null)
                            spaceIdx = i++;
                        sides[edgePos] = (int) spaceIdx;
                        continue;
                    }

                    foreach (var connectedPos in connected.Intersect(adjacentPositions))
                        sides[connectedPos] = i;

                    i++;
                }

                // If the sides were all connected, this atmosphere is intact
                if (i == 1)
                    continue;

                // If any of the sides that used to have this atmosphere are now exposed to space, remove
                // all the old atmosphere's cells.
                if (spaceIdx != null)
                {
                    foreach (var rpos in FindRoomContents(atmos))
                        _atmospheres.Remove(rpos);
                }

                // Split up each of the sides
                for (var j = 0; j < i; j++)
                {
                    // Find a position to represent this
                    var basePos = sides.First(p => p.Value == j).Key;

                    var connected = FindConnectedCells(basePos);

                    if(connected == null)
                        continue;

                    var newAtmos = new Atmosphere();

                    // TODO copy over contents of atmos

                    foreach (var cpos in connected)
                        _atmospheres[cpos] = newAtmos;
                }
            }
        }

        private void MergeAtmospheres(MapIndices pos)
        {
            var adjacent = new HashSet<Atmosphere>(GetAdjacentAtmospheres(pos).Values);

            // If this block doesn't separate two atmospheres, there's nothing to do
            if (adjacent.Count <= 1)
                return;

            // Fuse all adjacent atmospheres
            Atmosphere replacement;
            if (adjacent.Contains(null))
            {
                replacement = null;
            }
            else
            {
                replacement = new Atmosphere();
                foreach (var atmos in adjacent)
                {
                    // TODO Add atmos's gasses into replacement
                }
            }

            var allCells = new List<MapIndices> {pos};
            foreach (var atmos in adjacent)
                allCells.AddRange(FindRoomContents(atmos));

            foreach (var cellPos in allCells)
                _atmospheres[cellPos] = replacement;
        }

        private Dictionary<Direction, Atmosphere> GetAdjacentAtmospheres(MapIndices pos)
        {
            var sides = new Dictionary<Direction, Atmosphere>();
            foreach (var dir in Cardinal())
            {
                var side = Offset(pos, dir);
                if (IsObstructed(side))
                    continue;

                sides[dir] = GetAtmosphere(side);
            }

            return sides;
        }

        private static MapIndices Offset(MapIndices pos, Direction dir)
        {
            return pos + (MapIndices) CardinalToIntVec(dir);
        }

        private IEnumerable<MapIndices> FindRoomContents(Atmosphere atmos)
        {
            // TODO this is O(n) with respect to the map size, so solve this some other way
            var poses = new List<MapIndices>();
            foreach (var (pos, cellAtmos) in _atmospheres)
            {
                if (cellAtmos == atmos)
                    poses.Add(pos);
            }

            return poses;
        }

        private static IEnumerable<Direction> Cardinal() =>
            new[]
            {
                Direction.North, Direction.East, Direction.South, Direction.West
            };
    }

    internal class Atmosphere : IAtmosphere
    {
    }
}
