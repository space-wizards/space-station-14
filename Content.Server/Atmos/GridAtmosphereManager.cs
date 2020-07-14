using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Content.Server.GameObjects.Components.Atmos;
using Content.Server.Interfaces.Atmos;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.Atmos
{
    /// <inheritdoc cref="IGridAtmosphereManager"/>
    internal class GridAtmosphereManager : IGridAtmosphereManager
    {
        private readonly IMapGrid _grid;
        private readonly HashSet<ZoneAtmosphere> _zones = new HashSet<ZoneAtmosphere>();
        private readonly Dictionary<MapIndices, TileAtmosphere> _tiles = new Dictionary<MapIndices, TileAtmosphere>();

        private readonly HashSet<MapIndices> _invalidatedCoords = new HashSet<MapIndices>();

        public GridAtmosphereManager(IMapGrid grid)
        {
            _grid = grid;
        }


        public void Initialize()
        {
            RepopulateTiles();
            RepopulateZones();
        }

        private void RepopulateTiles()
        {
            _tiles.Clear();

            foreach (var tile in _grid.GetAllTiles())
            {
                _tiles.Add(tile.GridIndices, new TileAtmosphere(tile, GetVolumeForCells(1)));
            }
        }

        private void RepopulateZones()
        {
            foreach (var (indices, tile) in _tiles)
            {
                if (tile.Zone != null)
                    continue;

                var tiles = FindConnectedTiles(indices);
                var zone = new ZoneAtmosphere(this, tiles);

                foreach (var tileIndices in tiles)
                {
                    _tiles[tileIndices].Zone = zone;
                }
            }
        }

        /// <inheritdoc />
        public void Invalidate(MapIndices indices)
        {
            _invalidatedCoords.Add(indices);
        }

        private void Revalidate()
        {
            foreach (var indices in _invalidatedCoords)
            {
                /*if (IsSpace(indices))
                {
                    ClearAtmospheres(indices);
                }
                else if (IsZoneBlocked(indices))
                {
                    SplitAtmospheres(indices);
                } else if (IsAirBlocked(indices))
                {
                    // connect zones?
                }
                else
                {
                    MergeAtmospheres(indices);
                }*/
            }

            _invalidatedCoords.Clear();
        }

        /// <inheritdoc />
        public ZoneAtmosphere GetZone(MapIndices indices)
        {
            return !_tiles.TryGetValue(indices, out var tile) ? null : tile.Zone;
        }

        /// <inheritdoc />
        public TileAtmosphere GetTile(MapIndices indices)
        {
            return !_tiles.TryGetValue(indices, out var tile) ? null : tile;
        }

        /// <summary>
        /// Get the collection of grid cells connected to a given cell.
        /// </summary>
        /// <param name="start">The cell to start building the collection from.</param>
        /// <returns>
        /// <c>null</c>, if the cell is somehow connected to space. Otherwise, the
        /// collection of all cells connected to the starting cell (inclusive).
        /// </returns>
        private ISet<MapIndices> FindConnectedTiles(MapIndices start)
        {
            var inner = new HashSet<MapIndices>();
            var edge = new HashSet<MapIndices> {start};

            void Check(MapIndices pos, Direction dir)
            {
                pos = pos.Offset(dir);
                if (IsZoneBlocked(pos))
                    return;

                if (inner.Contains(pos))
                    return;

                edge.Add(pos);
            }

            // Basic inner/edge finder
            // The inner list is all the positions we know are empty, and take no further actions.
            // The edge list is all the positions for whom we have not searched their neighbours.
            // Move stuff from edge to inner, while adding adjacent empty locations to edge
            // When edge is empty, we have filled the entire room.

            while (edge.Count > 0)
            {
                var temp = new HashSet<MapIndices>(edge);
                edge.Clear();
                foreach (var pos in temp)
                {
                    inner.Add(pos);

                    if (IsSpace(pos))
                        return null;

                    foreach (var dir in Cardinal())
                    {
                        Check(pos, dir);
                    }
                }
            }

            return inner;
        }

        /// <inheritdoc />
        public bool IsZoneBlocked(MapIndices indices)
        {
            var ac = GetObstructingComponent(indices);
            return ac != null && ac.ZoneBlocked;
        }

        /// <inheritdoc />
        public bool IsAirBlocked(MapIndices indices)
        {
            var ac = GetObstructingComponent(indices);
            return ac != null && ac.AirBlocked;
        }

        /// <inheritdoc />
        public bool IsSpace(MapIndices indices)
        {
            // TODO ATMOS use ContentTileDefinition to define in YAML whether or not a tile is considered space
            return _grid.GetTileRef(indices).Tile.IsEmpty;
        }

        /// <inheritdoc />
        public Dictionary<Direction, ZoneAtmosphere> GetAdjacentZones(MapIndices indices)
        {
            var sides = new Dictionary<Direction, ZoneAtmosphere>();
            foreach (var dir in Cardinal())
            {
                var side = indices.Offset(dir);
                if (IsZoneBlocked(side))
                    continue;

                sides[dir] = GetZone(side);
            }

            return sides;
        }

        /// <inheritdoc />
        public Dictionary<Direction, TileAtmosphere> GetAdjacentTiles(MapIndices indices)
        {
            var sides = new Dictionary<Direction, TileAtmosphere>();
            foreach (var dir in Cardinal())
            {
                var side = indices.Offset(dir);
                sides[dir] = GetTile(side);
            }

            return sides;
        }

        /// <inheritdoc />
        public float GetVolumeForCells(int cellCount)
        {
            return _grid.TileSize * cellCount * Atmospherics.CellVolume;
        }

        public void Update(float frameTime)
        {
            if (_invalidatedCoords.Count != 0)
                Revalidate();
        }

        private AirtightComponent GetObstructingComponent(MapIndices indices)
        {
            foreach (var v in _grid.GetSnapGridCell(indices, SnapGridOffset.Center))
            {
                if (v.Owner.TryGetComponent<AirtightComponent>(out var ac))
                    return ac;
            }

            return null;
        }

        private static IEnumerable<Direction> Cardinal() =>
            new[]
            {
                Direction.North, Direction.East, Direction.South, Direction.West
            };

        public void Dispose()
        {

        }
    }
}
