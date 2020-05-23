using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Content.Server.GameObjects;
using Content.Server.GameObjects.Components.Atmos;
using Content.Server.Interfaces.Atmos;
using Content.Shared.Atmos;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Serialization;
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
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IPauseManager _pauseManager;
#pragma warning restore 649

        private readonly Dictionary<GridId, GridAtmosphereManager> _gridAtmosphereManagers =
            new Dictionary<GridId, GridAtmosphereManager>();

        public IGridAtmosphereManager GetGridAtmosphereManager(GridId grid)
        {
            if (_gridAtmosphereManagers.TryGetValue(grid, out var manager))
                return manager;

            if (!_mapManager.GridExists(grid))
                throw new ArgumentException("Cannot get atmosphere of missing grid", nameof(grid));

            manager = new GridAtmosphereManager(_mapManager.GetGrid(grid));
            _gridAtmosphereManagers[grid] = manager;
            return manager;
        }

        public IAtmosphere GetAtmosphere(ITransformComponent position) =>
            GetGridAtmosphereManager(position.GridID).GetAtmosphere(position.GridPosition);

        public void Update(float frameTime)
        {
            foreach (var (gridId, atmos) in _gridAtmosphereManagers)
            {
                if (_pauseManager.IsGridPaused(gridId))
                    continue;

                atmos.Update(frameTime);
            }
        }
    }

    internal class GridAtmosphereManager : IGridAtmosphereManager
    {
        // Arbitrarily define rooms as being 2.5m high
        private const float ROOM_HEIGHT = 2.5f;

        private readonly IMapGrid _grid;
        private Dictionary<GridCoordinates, ZoneAtmosphere> _atmospheres = new Dictionary<GridCoordinates, ZoneAtmosphere>();

        private HashSet<GridCoordinates> _invalidatedCoords = new HashSet<GridCoordinates>();

        public GridAtmosphereManager(IMapGrid grid)
        {
            _grid = grid;
        }

        public IAtmosphere GetAtmosphere(GridCoordinates coordinates) => GetZoneAtmosphere(coordinates);

        private ZoneAtmosphere GetZoneAtmosphere(GridCoordinates coordinates)
        {
            if (_atmospheres.TryGetValue(coordinates, out var atmosphere))
                return atmosphere;

            // If this is clearly space - such as someone moving round outside the station - just return immediately
            // and don't add anything to the grid map
            if (IsSpace(coordinates))
                return null;

            // If the block is obstructed:
            // If the air blocker is marked as 'use adjacent atmosphere' then the cell behaves as if
            // you got one of it's adjacent atmospheres. If not, it returns null.
            var obstruction = GetObstructingComponent(coordinates);
            if (obstruction != null)
                return obstruction.UseAdjacentAtmosphere ? GetAdjacentAtmospheres(coordinates).FirstOrDefault().Value : null;

            var connected = FindConnectedCells(coordinates);

            if (connected == null)
            {
                // Since this is on a floor tile, it's fairly likely this is a room with
                // a breach. As such, cache this position for sake of static objects like
                // air vents which call this constantly.
                _atmospheres[coordinates] = null;
                return null;
            }

            atmosphere = new ZoneAtmosphere(this, connected);
            // TODO: Not hardcode this
            var oneAtmosphere = 101325; // 1 atm in Pa
            var roomTemp = 293.15f; // 20c in k
            // Calculating moles to add: n = (PV)/(RT)
            var totalMoles = (oneAtmosphere * atmosphere.Volume) / (IAtmosphere.R * roomTemp);
            atmosphere.Add(new Gas("oxygen"), totalMoles * 0.2f, roomTemp);
            atmosphere.Add(new Gas("nitrogen"), totalMoles * 0.8f, roomTemp);

            foreach (var c in connected)
                _atmospheres[c] = atmosphere;

            return atmosphere;
        }

        public void Invalidate(GridCoordinates coordinates)
        {
            _invalidatedCoords.Add(coordinates);
        }

        private List<GridCoordinates> FindConnectedCells(GridCoordinates start)
        {
            var inner = new HashSet<GridCoordinates>();
            var edge = new HashSet<GridCoordinates> {start};

            // Basic inner/edge finder
            // The inner list is all the positions we know are empty, and take no further actions.
            // The edge list is all the positions for whom we have not searched their neighbours.
            // Move stuff from edge to inner, while adding adjacent empty locations to edge
            // When edge is empty, we have filled the entire room.

            while (edge.Count > 0)
            {
                var temp = new List<GridCoordinates>(edge);
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

            return new List<GridCoordinates>(inner);
        }

        private void Check(ICollection<GridCoordinates> inner, ISet<GridCoordinates> edges, GridCoordinates pos, Direction dir)
        {
            pos = pos.Offset(dir.ToVec());
            if (IsObstructed(pos))
                return;

            if (inner.Contains(pos))
                return;

            edges.Add(pos);
        }

        private bool IsObstructed(GridCoordinates coordinates) => GetObstructingComponent(coordinates) != null;

        private AirtightComponent GetObstructingComponent(GridCoordinates coordinates)
        {
            foreach (var v in _grid.GetSnapGridCell(coordinates, SnapGridOffset.Center))
            {
                if (v.Owner.TryGetComponent<AirtightComponent>(out var ac))
                    return ac;
            }

            return null;
        }

        private bool IsSpace(GridCoordinates coordinates)
        {
            // TODO use ContentTileDefinition to define in YAML whether or not a tile is considered space
            return _grid.GetTileRef(coordinates).Tile.IsEmpty;
        }

        public void Update(float frameTime)
        {
            if (_invalidatedCoords.Count != 0)
                Revalidate();
        }

        private void Revalidate()
        {
            foreach (var coordinates in _invalidatedCoords)
            {
                if (IsObstructed(coordinates))
                    SplitAtmospheres(coordinates);
                else
                    MergeAtmospheres(coordinates);
            }

            _invalidatedCoords.Clear();
        }

        private void SplitAtmospheres(GridCoordinates coordinates)
        {
            // Remove the now-covered atmosphere (if there was one)
            if (_atmospheres.TryGetValue(coordinates, out var coveredAtmos))
            {
                _atmospheres.Remove(coordinates);
                coveredAtmos.RemoveCell(coordinates);
            }

            // The collection of split atmosphere components - each one is a collection
            // of connected grid coordinates
            var sides = new List<List<GridCoordinates>>();

            foreach (var edgeCoordinate in Cardinal().Select(dir => coordinates.Offset(dir.ToVec())))
            {
                // If this is true, this edge is already contained in another edge's connected area
                if (sides.Any(collection => collection != null && collection.Contains(edgeCoordinate)))
                    continue;

                // If this is true, the edge has no atmosphere to split anyways
                if (IsObstructed(edgeCoordinate))
                    continue;

                // Otherwise, this is a new connected area (now that zones are split)
                var connected = FindConnectedCells(edgeCoordinate);
                // So add it to the map, and increase the number of split atmospheres
                sides.Add(connected);
            }

            // If the sides were all connected (or have no air), this atmosphere is intact
            if (sides.Count <= 1)
            {
                return;
            }

            // Split up each of the sides
            foreach(var connected in sides)
            {
                if (connected == null)
                    continue;

                var newAtmos = new ZoneAtmosphere(this, connected);

                // Copy over contents of atmos, scaling it down to maintain the partial pressure
                if (coveredAtmos != null)
                {
                    newAtmos.Temperature = coveredAtmos.Temperature;
                    foreach (var gas in coveredAtmos.Gasses)
                        newAtmos.SetQuantity(gas.Gas, gas.Quantity * newAtmos.Volume / coveredAtmos.Volume);
                }

                foreach (var cpos in connected)
                    _atmospheres[cpos] = newAtmos;
            }
        }

        private void MergeAtmospheres(GridCoordinates coordinates)
        {
            var adjacent = new HashSet<ZoneAtmosphere>(GetAdjacentAtmospheres(coordinates).Values);

            // If this block doesn't separate two atmospheres, there's no merging to do
            if (adjacent.Count <= 1)
            {
                var joinedAtmos = adjacent.First();
                // But this block was missing an atmosphere, so add one
                // and include this cell's volume in it
                joinedAtmos.AddCell(coordinates);
                _atmospheres[coordinates] = joinedAtmos;
                return;
            }

            var allCells = new List<GridCoordinates> {coordinates};
            foreach (var atmos in adjacent)
                allCells.AddRange(atmos.Cells);

            // Fuse all adjacent atmospheres
            ZoneAtmosphere replacement;
            if (adjacent.Contains(null))
            {
                replacement = null;
            }
            else
            {
                replacement = new ZoneAtmosphere(this, allCells);
                foreach (var atmos in adjacent)
                {
                    // Copy all the gasses across
                    foreach (var gas in atmos.Gasses)
                        replacement.Add(gas.Gas, gas.Quantity, atmos.Temperature);
                }
            }

            foreach (var cellPos in allCells)
                _atmospheres[cellPos] = replacement;
        }

        private Dictionary<Direction, ZoneAtmosphere> GetAdjacentAtmospheres(GridCoordinates coords)
        {
            var sides = new Dictionary<Direction, ZoneAtmosphere>();
            foreach (var dir in Cardinal())
            {
                var side = coords.Offset(dir.ToVec());
                if (IsObstructed(side))
                    continue;

                sides[dir] = GetZoneAtmosphere(side);
            }

            return sides;
        }

        private static IEnumerable<Direction> Cardinal() =>
            new[]
            {
                Direction.North, Direction.East, Direction.South, Direction.West
            };

        internal float GetVolumeForCells(int cellCount)
        {
            int scale = _grid.TileSize;
            return scale * scale * cellCount * ROOM_HEIGHT;
        }
    }

    internal class ZoneAtmosphere : Atmosphere
    {
        private readonly GridAtmosphereManager _parentGridManager;
        private readonly ISet<GridCoordinates> _cells;

        /// <summary>
        /// The collection of grid cells which are a part of this zone.
        /// </summary>
        /// <remarks>
        /// This should be kept in sync with the corresponding entries in <see cref="GridAtmosphereManager"/>.
        /// </remarks>
        public IEnumerable<GridCoordinates> Cells => _cells;

        /// <summary>
        /// The volume of this zone.
        /// </summary>
        /// <remarks>
        /// This is directly calculated from the number of cells in the zone.
        /// </remarks>
        public override float Volume => _parentGridManager.GetVolumeForCells(_cells.Count);

        public ZoneAtmosphere(GridAtmosphereManager parent, IEnumerable<GridCoordinates> cells)
        {
            _parentGridManager = parent;
            _cells = new HashSet<GridCoordinates>(cells);
            UpdateCached();
        }

        /// <summary>
        /// Add a cell to the zone.
        /// </summary>
        /// <remarks>
        /// This does not update the parent <see cref="GridAtmosphereManager"/>.
        /// </remarks>
        /// <param name="cell">The coordinates of the cell to add.</param>
        public void AddCell(GridCoordinates cell)
        {
            _cells.Add(cell);
            UpdateCached();
        }

        /// <summary>
        /// Remove a cell from the zone.
        /// </summary>
        /// <remarks>
        /// This does not update the parent <see cref="GridAtmosphereManager"/>.
        /// </remarks>
        /// <param name="cell">The coordinates of the cell to remove.</param>
        public void RemoveCell(GridCoordinates cell)
        {
            _cells.Remove(cell);
            UpdateCached();
        }
    }
}
