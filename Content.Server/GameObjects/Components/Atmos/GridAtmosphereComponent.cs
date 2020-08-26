#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Content.Server.Atmos;
using Content.Shared.Atmos;
using Content.Shared.Maps;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Map;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos
{
    /// <summary>
    ///     This is our SSAir equivalent.
    /// </summary>
    [ComponentReference(typeof(IGridAtmosphereComponent))]
    [RegisterComponent, Serializable]
    public class GridAtmosphereComponent : Component, IGridAtmosphereComponent
    {
        [Robust.Shared.IoC.Dependency] private IMapManager _mapManager = default!;

        /// <summary>
        ///     Check current execution time every n instances processed.
        /// </summary>
        private const int LagCheckIterations = 15;

        /// <summary>
        ///     Max milliseconds allowed for atmos updates.
        /// </summary>
        private const float LagCheckMaxMilliseconds = 5f;

        /// <summary>
        ///     How much time before atmos updates are ran.
        /// </summary>
        private const float AtmosTime = 1/26f;

        public override string Name => "GridAtmosphere";

        private float _timer = 0f;
        private Stopwatch _stopwatch = new Stopwatch();
        public int UpdateCounter { get; private set; } = 0;

        [ViewVariables]
        private readonly HashSet<ExcitedGroup> _excitedGroups = new HashSet<ExcitedGroup>(1000);

        [ViewVariables]
        private readonly Dictionary<MapIndices, TileAtmosphere> _tiles = new Dictionary<MapIndices, TileAtmosphere>(1000);

        [ViewVariables]
        private readonly HashSet<TileAtmosphere> _activeTiles = new HashSet<TileAtmosphere>(1000);

        [ViewVariables]
        private readonly HashSet<TileAtmosphere> _hotspotTiles = new HashSet<TileAtmosphere>(1000);

        [ViewVariables]
        private readonly HashSet<TileAtmosphere> _superconductivityTiles = new HashSet<TileAtmosphere>(1000);

        [ViewVariables]
        private readonly HashSet<MapIndices> _invalidatedCoords = new HashSet<MapIndices>(1000);

        [ViewVariables]
        private HashSet<TileAtmosphere> _highPressureDelta = new HashSet<TileAtmosphere>(1000);

        [ViewVariables]
        private ProcessState _state = ProcessState.TileEqualize;

        private enum ProcessState
        {
            TileEqualize,
            ActiveTiles,
            ExcitedGroups,
            HighPressureDelta,
            Hotspots,
            Superconductivity,
        }

        /// <inheritdoc />
        public void PryTile(MapIndices indices)
        {
            if (!Owner.TryGetComponent(out IMapGridComponent? mapGridComponent)) return;
            if (IsSpace(indices) || IsAirBlocked(indices)) return;

            var mapGrid = mapGridComponent.Grid;
            var tile = mapGrid.GetTileRef(indices).Tile;

            var tileDefinitionManager = IoCManager.Resolve<ITileDefinitionManager>();
            var tileDef = (ContentTileDefinition)tileDefinitionManager[tile.TypeId];

            var underplating = tileDefinitionManager["underplating"];
            mapGrid.SetTile(indices, new Tile(underplating.TileId));

            //Actually spawn the relevant tile item at the right position and give it some offset to the corner.
            var tileItem = IoCManager.Resolve<IServerEntityManager>().SpawnEntity(tileDef.ItemDropPrototypeName, new GridCoordinates(indices.X, indices.Y, mapGrid));
            tileItem.Transform.WorldPosition += (0.2f, 0.2f);
        }

        public override void Initialize()
        {
            base.Initialize();
            RepopulateTiles();
        }

        public override void OnAdd()
        {
            base.OnAdd();
            RepopulateTiles();
        }

        public void RepopulateTiles()
        {
            if (!Owner.TryGetComponent(out IMapGridComponent? mapGrid)) return;

            foreach (var tile in mapGrid.Grid.GetAllTiles())
            {
                if(!_tiles.ContainsKey(tile.GridIndices))
                    _tiles.Add(tile.GridIndices, new TileAtmosphere(this, tile.GridIndex, tile.GridIndices, new GasMixture(GetVolumeForCells(1)){Temperature = Atmospherics.T20C}));
            }

            foreach (var (_, tile) in _tiles.ToArray())
            {
                tile.UpdateAdjacent();
                tile.UpdateVisuals();
            }
        }

        /// <inheritdoc />
        public void Invalidate(MapIndices indices)
        {
            _invalidatedCoords.Add(indices);
        }

        private void Revalidate()
        {
            if (!Owner.TryGetComponent(out IMapGridComponent? mapGrid)) return;

            foreach (var indices in _invalidatedCoords.ToArray())
            {
                var tile = GetTile(indices);
                AddActiveTile(tile);

                if (tile == null)
                {
                    tile = new TileAtmosphere(this, mapGrid.Grid.Index, indices, new GasMixture(GetVolumeForCells(1)){Temperature = Atmospherics.T20C});
                    _tiles[indices] = tile;
                }

                if (IsSpace(indices))
                {
                    tile.Air = new GasMixture(GetVolumeForCells(1));
                    tile.Air.MarkImmutable();
                    _tiles[indices] = tile;

                } else if (IsAirBlocked(indices))
                {
                    tile.Air = null;
                }
                else
                {
                    var obs = GetObstructingComponent(indices);

                    if (obs != null)
                    {
                        if (tile.Air == null && obs.FixVacuum)
                        {
                            FixVacuum(tile.GridIndices);
                        }
                    }

                    tile.Air ??= new GasMixture(GetVolumeForCells(1)){Temperature = Atmospherics.T20C};
                }

                tile.UpdateAdjacent();
                tile.UpdateVisuals();

                foreach (var direction in Cardinal)
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
        public void FixVacuum(MapIndices indices)
        {
            if (!Owner.TryGetComponent(out IMapGridComponent? mapGrid)) return;
            var tile = GetTile(indices);
            if (tile?.GridIndex != mapGrid.Grid.Index) return;
            var adjacent = GetAdjacentTiles(indices);
            tile.Air = new GasMixture(GetVolumeForCells(1)){Temperature = Atmospherics.T20C};
            _tiles[indices] = tile;

            var ratio = 1f / adjacent.Count;

            foreach (var (direction, adj) in adjacent)
            {
                var mix = adj.Air.RemoveRatio(ratio);
                tile.Air.Merge(mix);
                adj.Air.Merge(mix);
            }
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddActiveTile(TileAtmosphere? tile)
        {
            if (!Owner.TryGetComponent(out IMapGridComponent? mapGrid)) return;
            if (tile?.GridIndex != mapGrid.Grid.Index || tile?.Air == null) return;
            tile.Excited = true;
            _activeTiles.Add(tile);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveActiveTile(TileAtmosphere? tile)
        {
            if (tile == null) return;
            _activeTiles.Remove(tile);
            tile.Excited = false;
            tile.ExcitedGroup?.Dispose();
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddHotspotTile(TileAtmosphere? tile)
        {
            if (!Owner.TryGetComponent(out IMapGridComponent? mapGrid)) return;
            if (tile?.GridIndex != mapGrid.Grid.Index || tile?.Air == null) return;
            _hotspotTiles.Add(tile);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveHotspotTile(TileAtmosphere? tile)
        {
            if (tile == null) return;
            _hotspotTiles.Remove(tile);
        }

        public void AddSuperconductivityTile(TileAtmosphere? tile)
        {
            if (!Owner.TryGetComponent(out IMapGridComponent? mapGrid)) return;
            if (tile?.GridIndex != mapGrid.Grid.Index) return;
            _superconductivityTiles.Add(tile);
        }

        public void RemoveSuperconductivityTile(TileAtmosphere? tile)
        {
            if (tile == null) return;
            _superconductivityTiles.Remove(tile);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddHighPressureDelta(TileAtmosphere? tile)
        {
            if (!Owner.TryGetComponent(out IMapGridComponent? mapGrid)) return;
            if (tile?.GridIndex != mapGrid.Grid.Index) return;
            _highPressureDelta.Add(tile);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasHighPressureDelta(TileAtmosphere tile)
        {
            return _highPressureDelta.Contains(tile);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddExcitedGroup(ExcitedGroup excitedGroup)
        {
            _excitedGroups.Add(excitedGroup);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveExcitedGroup(ExcitedGroup excitedGroup)
        {
            _excitedGroups.Remove(excitedGroup);
        }

        /// <inheritdoc />
        public TileAtmosphere? GetTile(GridCoordinates coordinates)
        {
            return GetTile(coordinates.ToMapIndices(_mapManager));
        }

        /// <inheritdoc />
        public TileAtmosphere? GetTile(MapIndices indices)
        {
            if (!Owner.TryGetComponent(out IMapGridComponent? mapGrid)) return null;

            if (_tiles.TryGetValue(indices, out var tile)) return tile;

            // We don't have that tile!
            if (IsSpace(indices))
            {
                var space = new TileAtmosphere(this, mapGrid.Grid.Index, indices, new GasMixture(int.MaxValue){Temperature = Atmospherics.TCMB});
                space.Air.MarkImmutable();
                return space;
            }

            return null;
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
            if (!Owner.TryGetComponent(out IMapGridComponent? mapGrid)) return default;

            return mapGrid.Grid.GetTileRef(indices).Tile.IsEmpty;
        }

        public Dictionary<Direction, TileAtmosphere> GetAdjacentTiles(MapIndices indices, bool includeAirBlocked = false)
        {
            var sides = new Dictionary<Direction, TileAtmosphere>();
            foreach (var dir in Cardinal)
            {
                var side = indices.Offset(dir);
                var tile = GetTile(side);
                if (tile != null && (tile.Air != null || includeAirBlocked))
                    sides[dir] = tile;
            }

            return sides;
        }

        /// <inheritdoc />
        public int HighPressureDeltaCount => _highPressureDelta.Count;

        public long EqualizationQueueCycleControl { get; set; }

        /// <inheritdoc />
        public float GetVolumeForCells(int cellCount)
        {
            if (!Owner.TryGetComponent(out IMapGridComponent? mapGrid)) return default;

            return mapGrid.Grid.TileSize * cellCount * Atmospherics.CellVolume;
        }

        /// <inheritdoc />
        public void Update(float frameTime)
        {
            _timer += frameTime;

            if (_invalidatedCoords.Count != 0)
                Revalidate();

            if (_timer < AtmosTime)
                return;

            // We subtract it so it takes lost time into account.
            _timer -= AtmosTime;

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
                    _state = ProcessState.Hotspots;
                    break;
                case ProcessState.Hotspots:
                    ProcessHotspots();
                    _state = ProcessState.Superconductivity;
                    break;
                case ProcessState.Superconductivity:
                    ProcessSuperconductivity();
                    _state = ProcessState.TileEqualize;
                    break;
            }

            UpdateCounter++;
        }

        public void ProcessTileEqualize()
        {
            _stopwatch.Restart();

            var number = 0;
            foreach (var tile in _activeTiles.ToArray())
            {
                tile.EqualizePressureInZone(UpdateCounter);

                if (number++ < LagCheckIterations) continue;
                number = 0;
                // Process the rest next time.
                if (_stopwatch.Elapsed.TotalMilliseconds >= LagCheckMaxMilliseconds)
                    return;
            }
        }

        public void ProcessActiveTiles()
        {
            _stopwatch.Restart();

            var number = 0;
            foreach (var tile in _activeTiles.ToArray())
            {
                tile.ProcessCell(UpdateCounter);

                if (number++ < LagCheckIterations) continue;
                number = 0;
                // Process the rest next time.
                if (_stopwatch.Elapsed.TotalMilliseconds >= LagCheckMaxMilliseconds)
                    return;
            }
        }

        public void ProcessExcitedGroups()
        {
            _stopwatch.Restart();

            var number = 0;
            foreach (var excitedGroup in _excitedGroups.ToArray())
            {
                excitedGroup.BreakdownCooldown++;
                excitedGroup.DismantleCooldown++;

                if(excitedGroup.BreakdownCooldown > Atmospherics.ExcitedGroupBreakdownCycles)
                    excitedGroup.SelfBreakdown();

                else if(excitedGroup.DismantleCooldown > Atmospherics.ExcitedGroupsDismantleCycles)
                    excitedGroup.Dismantle();

                if (number++ < LagCheckIterations) continue;
                number = 0;
                // Process the rest next time.
                if (_stopwatch.Elapsed.TotalMilliseconds >= LagCheckMaxMilliseconds)
                    return;
            }
        }

        public void ProcessHighPressureDelta()
        {
            _stopwatch.Restart();

            var number = 0;
            foreach (var tile in _highPressureDelta.ToArray())
            {
                tile.HighPressureMovements();
                tile.PressureDifference = 0f;
                tile.PressureSpecificTarget = null;
                _highPressureDelta.Remove(tile);

                if (number++ < LagCheckIterations) continue;
                number = 0;
                // Process the rest next time.
                if (_stopwatch.Elapsed.TotalMilliseconds >= LagCheckMaxMilliseconds)
                    return;
            }
        }

        private void ProcessHotspots()
        {
            _stopwatch.Restart();

            var number = 0;
            foreach (var hotspot in _hotspotTiles.ToArray())
            {
                hotspot.ProcessHotspot();

                if (number++ < LagCheckIterations) continue;
                number = 0;
                // Process the rest next time.
                if (_stopwatch.Elapsed.TotalMilliseconds >= LagCheckMaxMilliseconds)
                    return;
            }
        }

        private void ProcessSuperconductivity()
        {
            _stopwatch.Restart();

            var number = 0;
            foreach (var superconductivity in _superconductivityTiles.ToArray())
            {
                superconductivity.Superconduct();

                if (number++ < LagCheckIterations) continue;
                number = 0;
                // Process the rest next time.
                if (_stopwatch.Elapsed.TotalMilliseconds >= LagCheckMaxMilliseconds)
                    return;
            }
        }

        private AirtightComponent? GetObstructingComponent(MapIndices indices)
        {
            if (!Owner.TryGetComponent(out IMapGridComponent? mapGrid)) return default;

            foreach (var v in mapGrid.Grid.GetSnapGridCell(indices, SnapGridOffset.Center))
            {
                if (v.Owner.TryGetComponent<AirtightComponent>(out var ac))
                    return ac;
            }

            return null;
        }

        private static readonly Direction[] Cardinal =
            new []
            {
                Direction.North, Direction.East, Direction.South, Direction.West
            };

        public void Dispose()
        {

        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            if (serializer.Reading &&
                Owner.TryGetComponent(out IMapGridComponent? mapGrid))
            {
                var gridId = mapGrid.Grid.Index;

                if (!serializer.TryReadDataField("uniqueMixes", out List<GasMixture>? uniqueMixes) ||
                    !serializer.TryReadDataField("tiles", out Dictionary<MapIndices, int>? tiles))
                    return;

                _tiles.Clear();

                foreach (var (indices, mix) in tiles!)
                {
                    _tiles.Add(indices, new TileAtmosphere(this, gridId, indices, (GasMixture)uniqueMixes![mix].Clone()));
                    Invalidate(indices);
                }
            }
            else if (serializer.Writing)
            {
                var uniqueMixes = new List<GasMixture>();
                var uniqueMixHash = new Dictionary<GasMixture, int>();
                var tiles = new Dictionary<MapIndices, int>();
                foreach (var (indices, tile) in _tiles)
                {
                    if (tile.Air == null) continue;

                    if (uniqueMixHash.TryGetValue(tile.Air, out var index))
                    {
                        tiles[indices] = index;
                        continue;
                    }

                    uniqueMixes.Add(tile.Air);
                    var newIndex = uniqueMixes.Count - 1;
                    uniqueMixHash[tile.Air] = newIndex;
                    tiles[indices] = newIndex;
                }

                serializer.DataField(ref uniqueMixes, "uniqueMixes", new List<GasMixture>());
                serializer.DataField(ref tiles, "tiles", new Dictionary<MapIndices, int>());
            }
        }

        public IEnumerator<TileAtmosphere> GetEnumerator()
        {
            return _tiles.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        public void BurnTile(MapIndices gridIndices)
        {
            // TODO ATMOS
        }
    }
}
