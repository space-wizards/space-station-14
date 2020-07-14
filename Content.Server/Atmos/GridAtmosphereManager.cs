using System;
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
        private readonly HashSet<WeakReference<ExcitedGroup>> _excitedGroups = new HashSet<WeakReference<ExcitedGroup>>();
        private readonly Dictionary<MapIndices, TileAtmosphere> _tiles = new Dictionary<MapIndices, TileAtmosphere>();
        private readonly HashSet<TileAtmosphere> _activeTiles = new HashSet<TileAtmosphere>();

        private readonly HashSet<MapIndices> _invalidatedCoords = new HashSet<MapIndices>();

        public GridAtmosphereManager(IMapGrid grid)
        {
            _grid = grid;
        }


        public void Initialize()
        {
            RepopulateTiles();
        }

        private void RepopulateTiles()
        {
            _tiles.Clear();

            foreach (var tile in _grid.GetAllTiles())
            {
                _tiles.Add(tile.GridIndices, new TileAtmosphere(this, tile, GetVolumeForCells(1)));
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
        public void AddActiveTile(MapIndices indices)
        {
            _activeTiles.Add(_tiles[indices]);
        }

        /// <inheritdoc />
        public void RemoveActiveTile(MapIndices indices)
        {
            _activeTiles.Remove(_tiles[indices]);
        }

        /// <inheritdoc />
        public void AddExcitedGroup(WeakReference<ExcitedGroup> excitedGroup)
        {
            _excitedGroups.Add(excitedGroup);
        }

        /// <inheritdoc />
        public void RemoveExcitedGroup(WeakReference<ExcitedGroup> excitedGroup)
        {
            _excitedGroups.Remove(excitedGroup);
        }

        /// <inheritdoc />
        public TileAtmosphere GetTile(MapIndices indices)
        {
            return !_tiles.TryGetValue(indices, out var tile) ? null : tile;
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
