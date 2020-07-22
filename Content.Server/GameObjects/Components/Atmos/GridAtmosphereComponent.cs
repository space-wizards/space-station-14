using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Content.Server.Atmos;
using Content.Shared.Atmos;
using Content.Shared.Maps;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Map;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Atmos
{
    [RegisterComponent, Serializable]
    public class GridAtmosphereComponent : Component
    {
        public override string Name => "GridAtmosphere";

        private int _timer = 0;
        private int _updateCounter = 0;
        private IMapGrid _grid;
        private readonly HashSet<ExcitedGroup> _excitedGroups = new HashSet<ExcitedGroup>(1000);
        private readonly Dictionary<MapIndices, TileAtmosphere> _tiles = new Dictionary<MapIndices, TileAtmosphere>(1000);
        private readonly HashSet<TileAtmosphere> _activeTiles = new HashSet<TileAtmosphere>(1000);
        private readonly HashSet<MapIndices> _invalidatedCoords = new HashSet<MapIndices>(1000);

        private ProcessState _state = ProcessState.TileEqualize;

        private HashSet<TileAtmosphere> _highPressureDelta = new HashSet<TileAtmosphere>(1000);

        private enum ProcessState
        {
            TileEqualize,
            ActiveTiles,
            ExcitedGroups,
            HighPressureDelta,
        }

        public void PryTile(MapIndices indices)
        {
            if (IsSpace(indices) || IsAirBlocked(indices)) return;

            var tile = _grid.GetTileRef(indices).Tile;

            var tileDefinitionManager = IoCManager.Resolve<ITileDefinitionManager>();
            var tileDef = (ContentTileDefinition)tileDefinitionManager[tile.TypeId];

            var underplating = tileDefinitionManager["underplating"];
            _grid.SetTile(indices, new Tile(underplating.TileId));

            //Actually spawn the relevant tile item at the right position and give it some offset to the corner.
            var tileItem = IoCManager.Resolve<IServerEntityManager>().SpawnEntity(tileDef.ItemDropPrototypeName, new GridCoordinates(indices.X, indices.Y, _grid));
            tileItem.Transform.WorldPosition += (0.2f, 0.2f);
        }

        public override void Initialize()
        {
            // TODO ATMOS Not repopulate tiles here

            base.Initialize();

            _grid = Owner.GetComponent<IMapGridComponent>().Grid;

            if(_tiles.Count == 0)
                RepopulateTiles();
        }

        public void RepopulateTiles()
        {
            _tiles.Clear();

            foreach (var tile in _grid.GetAllTiles(false))
            {
                _tiles.Add(tile.GridIndices, new TileAtmosphere(this, tile.GridIndex, tile.GridIndices, null));
            }

            foreach (var (_, tile) in _tiles)
            {
                tile.UpdateAdjacent();
                tile.UpdateVisuals();
            }
        }

        public void Invalidate(MapIndices indices)
        {
            _invalidatedCoords.Add(indices);
        }

        private void Revalidate()
        {
            foreach (var indices in _invalidatedCoords)
            {
                var tile = GetTile(indices);
                AddActiveTile(tile);

                if (tile == null)
                {
                    tile = new TileAtmosphere(this, _grid.Index, indices, null);
                    _tiles.Add(indices, tile);
                }

                if (IsSpace(indices))
                {
                    tile.Air = new GasMixture(GetVolumeForCells(1));
                    tile.Air.MarkImmutable();
                } else if (IsAirBlocked(indices))
                {
                    tile.Air = null;
                }
                else
                {
                    tile.Air ??= new GasMixture(GetVolumeForCells(1));
                }

                tile.UpdateAdjacent();
                tile.UpdateVisuals();

                foreach (var direction in Cardinal())
                {
                    var otherIndices = indices.Offset(direction);
                    var otherTile = GetTile(otherIndices);
                    AddActiveTile(otherTile);
                    otherTile?.UpdateAdjacent(direction.GetOpposite());
                }
            }

            _invalidatedCoords.Clear();
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddActiveTile(TileAtmosphere tile)
        {
            if (tile?.GridIndex != _grid.Index) return;
            _activeTiles.Add(tile);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveActiveTile(TileAtmosphere tile)
        {
            _activeTiles.Remove(tile);
        }

        /// <inheritdoc />
        public void AddHighPressureDelta(TileAtmosphere tile)
        {
            if (tile?.GridIndex == _grid.Index) return;
            _highPressureDelta.Add(tile);
        }

        /// <inheritdoc />
        public bool HasHighPressureDelta(TileAtmosphere tile)
        {
            return _highPressureDelta.Contains(tile);
        }

        /// <inheritdoc />
        public void AddExcitedGroup(ExcitedGroup excitedGroup)
        {
            _excitedGroups.Add(excitedGroup);
        }

        /// <inheritdoc />
        public void RemoveExcitedGroup(ExcitedGroup excitedGroup)
        {
            _excitedGroups.Remove(excitedGroup);
        }

        /// <inheritdoc />
        public TileAtmosphere GetTile(MapIndices indices)
        {
            return !_tiles.TryGetValue(indices, out var tile) ? null : tile;
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
        public int HighPressureDeltaCount => _highPressureDelta.Count;

        /// <inheritdoc />
        public float GetVolumeForCells(int cellCount)
        {
            return _grid.TileSize * cellCount * Atmospherics.CellVolume;
        }

        public void Update(float frameTime)
        {
            _timer += 1;

            if (_invalidatedCoords.Count != 0)
                Revalidate();

            if (_timer < 2)
                return;

            _timer = 0;

            switch (_state)
            {
                case ProcessState.TileEqualize:
                    ProcessTileEqualize();
                    _state = ProcessState.ActiveTiles;
                    return;
                case ProcessState.ActiveTiles:
                    ProcessActiveTiles();
                    _state = ProcessState.ExcitedGroups;
                    return;
                case ProcessState.ExcitedGroups:
                    ProcessExcitedGroups();
                    _state = ProcessState.HighPressureDelta;
                    return;
                case ProcessState.HighPressureDelta:
                    ProcessHighPressureDelta();
                    _state = ProcessState.TileEqualize;
                    break;
            }

            _updateCounter++;
        }

        public void ProcessHighPressureDelta()
        {
            foreach (var tile in _highPressureDelta.ToArray())
            {
                tile.HighPressureMovements();
                tile.PressureDifference = 0f;
                tile.PressureSpecificTarget = null;
                _highPressureDelta.Remove(tile);
            }
        }

        public void ProcessTileEqualize()
        {
            foreach (var tile in _activeTiles.ToArray())
            {
                tile.EqualizePressureInZone(_updateCounter);
            }
        }

        public void ProcessActiveTiles()
        {
            foreach (var tile in _activeTiles.ToArray())
            {
                tile.ProcessCell(_updateCounter);
            }
        }

        public void ProcessExcitedGroups()
        {
            foreach (var excitedGroup in _excitedGroups.ToArray())
            {
                excitedGroup.BreakdownCooldown++;
                excitedGroup.DismantleCooldown++;

                if(excitedGroup.BreakdownCooldown > Atmospherics.ExcitedGroupBreakdownCycles)
                    excitedGroup.SelfBreakdown();

                else if(excitedGroup.DismantleCooldown > Atmospherics.ExcitedGroupsDismantleCycles)
                    excitedGroup.Dismantle();
            }
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

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            if (serializer.Reading)
            {
                var gridId = Owner.GetComponent<IMapGridComponent>().Grid.Index;

                if (!serializer.TryReadDataField("uniqueMixes", out List<GasMixture> uniqueMixes) ||
                    !serializer.TryReadDataField("tiles", out Dictionary<MapIndices, int> tiles))
                    return;
                foreach (var (indices, mix) in tiles)
                {
                    _tiles.Add(indices, new TileAtmosphere(this, gridId, indices, (GasMixture)uniqueMixes[mix].Clone()));
                    Invalidate(indices);
                }
            } else if (serializer.Writing)
            {
                var uniqueMixes = new List<GasMixture>();
                var tiles = new Dictionary<MapIndices, int>();
                foreach (var (indices, tile) in _tiles)
                {
                    if (tile.Air == null) continue;
                    uniqueMixes.Add(tile.Air);
                    tiles[indices] = uniqueMixes.Count - 1;
                }

                serializer.DataField(ref uniqueMixes, "uniqueMixes", new List<GasMixture>());
                serializer.DataField(ref tiles, "tiles", new Dictionary<MapIndices, int>());
            }
        }
    }
}
