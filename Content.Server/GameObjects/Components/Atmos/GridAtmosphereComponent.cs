#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.Atmos.Piping;
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.GameObjects.EntitySystems.Atmos;
using Content.Shared.Atmos;
using Content.Shared.Maps;
using Robust.Server.GameObjects.EntitySystems.TileLookup;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Map;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Log;
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
        [Robust.Shared.IoC.Dependency] private ITileDefinitionManager _tileDefinitionManager = default!;
        [Robust.Shared.IoC.Dependency] private IServerEntityManager _serverEntityManager = default!;

        public GridTileLookupSystem GridTileLookupSystem { get; private set; } = default!;
        internal GasTileOverlaySystem GasTileOverlaySystem { get; private set; } = default!;
        public AtmosphereSystem AtmosphereSystem { get; private set; } = default!;

        /// <summary>
        ///     Check current execution time every n instances processed.
        /// </summary>
        private const int LagCheckIterations = 30;

        /// <summary>
        ///     Max milliseconds allowed for atmos updates.
        /// </summary>
        private const float LagCheckMaxMilliseconds = 5f;

        /// <summary>
        ///     How much time before atmos updates are ran.
        /// </summary>
        private const float AtmosTime = 1/26f;

        public override string Name => "GridAtmosphere";

        private bool _paused = false;
        private float _timer = 0f;
        private Stopwatch _stopwatch = new Stopwatch();
        private GridId _gridId;

        [ViewVariables]
        public int UpdateCounter { get; private set; } = 0;

        [ViewVariables]
        private double _tileEqualizeLastProcess;

        [ViewVariables]
        private readonly HashSet<ExcitedGroup> _excitedGroups = new HashSet<ExcitedGroup>(1000);

        [ViewVariables]
        private int ExcitedGroupCount => _excitedGroups.Count;

        [ViewVariables]
        private double _excitedGroupLastProcess;

        [ViewVariables]
        protected readonly Dictionary<Vector2i, TileAtmosphere> Tiles = new Dictionary<Vector2i, TileAtmosphere>(1000);

        [ViewVariables]
        private readonly HashSet<TileAtmosphere> _activeTiles = new HashSet<TileAtmosphere>(1000);

        [ViewVariables]
        private int ActiveTilesCount => _activeTiles.Count;

        [ViewVariables]
        private double _activeTilesLastProcess;

        [ViewVariables]
        private readonly HashSet<TileAtmosphere> _hotspotTiles = new HashSet<TileAtmosphere>(1000);

        [ViewVariables]
        private int HotspotTilesCount => _hotspotTiles.Count;

        [ViewVariables]
        private double _hotspotsLastProcess;

        [ViewVariables]
        private readonly HashSet<TileAtmosphere> _superconductivityTiles = new HashSet<TileAtmosphere>(1000);

        [ViewVariables]
        private int SuperconductivityTilesCount => _superconductivityTiles.Count;

        [ViewVariables]
        private double _superconductivityLastProcess;

        [ViewVariables]
        private readonly HashSet<Vector2i> _invalidatedCoords = new HashSet<Vector2i>(1000);

        [ViewVariables]
        private int InvalidatedCoordsCount => _invalidatedCoords.Count;

        [ViewVariables]
        private HashSet<TileAtmosphere> _highPressureDelta = new HashSet<TileAtmosphere>(1000);

        [ViewVariables]
        private int HighPressureDeltaCount => _highPressureDelta.Count;

        [ViewVariables]
        private double _highPressureDeltaLastProcess;

        [ViewVariables]
        private readonly HashSet<IPipeNet> _pipeNets = new HashSet<IPipeNet>();

        [ViewVariables]
        private double _pipeNetLastProcess;

        [ViewVariables]
        private readonly HashSet<PipeNetDeviceComponent> _pipeNetDevices = new HashSet<PipeNetDeviceComponent>();

        [ViewVariables]
        private double _pipeNetDevicesLastProcess;

        [ViewVariables]
        private Queue<TileAtmosphere> _currentRunTiles = new Queue<TileAtmosphere>();

        [ViewVariables]
        private Queue<ExcitedGroup> _currentRunExcitedGroups = new Queue<ExcitedGroup>();

        [ViewVariables]
        private Queue<IPipeNet> _currentRunPipeNet = new Queue<IPipeNet>();

        [ViewVariables]
        private Queue<PipeNetDeviceComponent> _currentRunPipeNetDevice = new Queue<PipeNetDeviceComponent>();

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
            PipeNet,
            PipeNetDevices,
        }

        /// <inheritdoc />
        public virtual void PryTile(Vector2i indices)
        {
            if (IsSpace(indices) || IsAirBlocked(indices)) return;

            indices.PryTile(_gridId, _mapManager, _tileDefinitionManager, _serverEntityManager);
        }

        public override void Initialize()
        {
            base.Initialize();
            GridTileLookupSystem = EntitySystem.Get<GridTileLookupSystem>();
            GasTileOverlaySystem = EntitySystem.Get<GasTileOverlaySystem>();
            AtmosphereSystem = EntitySystem.Get<AtmosphereSystem>();

            RepopulateTiles();
        }

        public override void OnAdd()
        {
            base.OnAdd();

            if (Owner.TryGetComponent(out IMapGridComponent? mapGrid))
                _gridId = mapGrid.GridIndex;
        }

        public virtual void RepopulateTiles()
        {
            if (!Owner.TryGetComponent(out IMapGridComponent? mapGrid)) return;

            foreach (var tile in mapGrid.Grid.GetAllTiles())
            {
                if(!Tiles.ContainsKey(tile.GridIndices))
                    Tiles.Add(tile.GridIndices, new TileAtmosphere(this, tile.GridIndex, tile.GridIndices, new GasMixture(GetVolumeForCells(1), AtmosphereSystem){Temperature = Atmospherics.T20C}));

                Invalidate(tile.GridIndices);
            }

            foreach (var (_, tile) in Tiles.ToArray())
            {
                tile.UpdateAdjacent();
                tile.UpdateVisuals();
            }
        }

        /// <inheritdoc />
        public virtual void Invalidate(Vector2i indices)
        {
            _invalidatedCoords.Add(indices);
        }

        protected virtual void Revalidate()
        {
            foreach (var indices in _invalidatedCoords.ToArray())
            {
                var tile = GetTile(indices);

                if (tile == null)
                {
                    tile = new TileAtmosphere(this, _gridId, indices, new GasMixture(GetVolumeForCells(1), AtmosphereSystem){Temperature = Atmospherics.T20C});
                    Tiles[indices] = tile;
                }

                if (IsSpace(indices))
                {
                    tile.Air = new GasMixture(GetVolumeForCells(1), AtmosphereSystem);
                    tile.Air.MarkImmutable();
                    Tiles[indices] = tile;

                } else if (IsAirBlocked(indices))
                {
                    tile.Air = null;
                }
                else
                {
                    if (tile.Air == null && NeedsVacuumFixing(indices))
                    {
                        FixVacuum(tile.GridIndices);
                    }

                    // Tile used to be space, but isn't anymore.
                    if (tile.Air?.Immutable ?? false)
                    {
                        tile.Air = null;
                    }

                    tile.Air ??= new GasMixture(GetVolumeForCells(1), AtmosphereSystem){Temperature = Atmospherics.T20C};
                }

                AddActiveTile(tile);
                tile.BlockedAirflow = GetBlockedDirections(indices);

                // TODO ATMOS: Query all the contents of this tile (like walls) and calculate the correct thermal conductivity
                tile.ThermalConductivity = tile.Tile?.Tile.GetContentTileDefinition().ThermalConductivity ?? 0.5f;
                tile.UpdateAdjacent();
                tile.UpdateVisuals();

                for (var i = 0; i < Atmospherics.Directions; i++)
                {
                    var direction = (AtmosDirection) (1 << i);
                    var otherIndices = indices.Offset(direction.ToDirection());
                    var otherTile = GetTile(otherIndices);
                    AddActiveTile(otherTile);
                }
            }

            _invalidatedCoords.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateAdjacentBits(Vector2i indices)
        {
            GetTile(indices)?.UpdateAdjacent();
        }

        /// <inheritdoc />
        public virtual void FixVacuum(Vector2i indices)
        {
            var tile = GetTile(indices);
            if (tile?.GridIndex != _gridId) return;
            var adjacent = GetAdjacentTiles(indices);
            tile.Air = new GasMixture(GetVolumeForCells(1), AtmosphereSystem){Temperature = Atmospherics.T20C};
            Tiles[indices] = tile;

            var ratio = 1f / adjacent.Count;

            foreach (var (_, adj) in adjacent)
            {
                var mix = adj.Air.RemoveRatio(ratio);
                tile.Air.Merge(mix);
                adj.Air.Merge(mix);
            }
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void AddActiveTile(TileAtmosphere? tile)
        {
            if (tile?.GridIndex != _gridId) return;
            tile.Excited = true;
            _activeTiles.Add(tile);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void RemoveActiveTile(TileAtmosphere? tile, bool disposeGroup = true)
        {
            if (tile == null) return;
            _activeTiles.Remove(tile);
            tile.Excited = false;
            if(disposeGroup)
                tile.ExcitedGroup?.Dispose();
            else
                tile.ExcitedGroup?.RemoveTile(tile);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void AddHotspotTile(TileAtmosphere? tile)
        {
            if (tile?.GridIndex != _gridId || tile?.Air == null) return;
            _hotspotTiles.Add(tile);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void RemoveHotspotTile(TileAtmosphere? tile)
        {
            if (tile == null) return;
            _hotspotTiles.Remove(tile);
        }

        public virtual void AddSuperconductivityTile(TileAtmosphere? tile)
        {
            if (tile?.GridIndex != _gridId) return;
            _superconductivityTiles.Add(tile);
        }

        public virtual void RemoveSuperconductivityTile(TileAtmosphere? tile)
        {
            if (tile == null) return;
            _superconductivityTiles.Remove(tile);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void AddHighPressureDelta(TileAtmosphere? tile)
        {
            if (tile?.GridIndex != _gridId) return;
            _highPressureDelta.Add(tile);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool HasHighPressureDelta(TileAtmosphere tile)
        {
            return _highPressureDelta.Contains(tile);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void AddExcitedGroup(ExcitedGroup excitedGroup)
        {
            _excitedGroups.Add(excitedGroup);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void RemoveExcitedGroup(ExcitedGroup excitedGroup)
        {
            _excitedGroups.Remove(excitedGroup);
        }

        public virtual void AddPipeNet(IPipeNet pipeNet)
        {
            _pipeNets.Add(pipeNet);
        }

        public virtual void RemovePipeNet(IPipeNet pipeNet)
        {
            _pipeNets.Remove(pipeNet);
        }

        public virtual void AddPipeNetDevice(PipeNetDeviceComponent pipeNetDevice)
        {
            _pipeNetDevices.Add(pipeNetDevice);
        }

        public virtual void RemovePipeNetDevice(PipeNetDeviceComponent pipeNetDevice)
        {
            _pipeNetDevices.Remove(pipeNetDevice);
        }

        /// <inheritdoc />
        public virtual TileAtmosphere? GetTile(EntityCoordinates coordinates, bool createSpace = true)
        {
            return GetTile(coordinates.ToVector2i(_serverEntityManager, _mapManager), createSpace);
        }

        /// <inheritdoc />
        public virtual TileAtmosphere? GetTile(Vector2i indices, bool createSpace = true)
        {
            if (Tiles.TryGetValue(indices, out var tile)) return tile;

            // We don't have that tile!
            if (IsSpace(indices) && createSpace)
            {
                return new TileAtmosphere(this, _gridId, indices, new GasMixture(GetVolumeForCells(1), AtmosphereSystem){Temperature = Atmospherics.TCMB}, true);
            }

            return null;
        }

        /// <inheritdoc />
        public bool IsAirBlocked(Vector2i indices, AtmosDirection direction = AtmosDirection.All)
        {
            foreach (var obstructingComponent in GetObstructingComponents(indices))
            {
                if (!obstructingComponent.AirBlocked)
                    continue;

                if (obstructingComponent.AirBlockedDirection.HasFlag(direction))
                    return true;
            }

            return false;
        }

        /// <inheritdoc />
        public virtual bool IsSpace(Vector2i indices)
        {
            // TODO ATMOS use ContentTileDefinition to define in YAML whether or not a tile is considered space
            if (!Owner.TryGetComponent(out IMapGridComponent? mapGrid)) return default;

            return mapGrid.Grid.GetTileRef(indices).Tile.IsEmpty;
        }

        public Dictionary<AtmosDirection, TileAtmosphere> GetAdjacentTiles(Vector2i indices, bool includeAirBlocked = false)
        {
            var sides = new Dictionary<AtmosDirection, TileAtmosphere>();
            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var direction = (AtmosDirection) (1 << i);
                var side = indices.Offset(direction.ToDirection());
                var tile = GetTile(side);
                if (tile != null && (tile.Air != null || includeAirBlocked))
                    sides[direction] = tile;
            }

            return sides;
        }

        public long EqualizationQueueCycleControl { get; set; }

        /// <inheritdoc />
        public float GetVolumeForCells(int cellCount)
        {
            if (!Owner.TryGetComponent(out IMapGridComponent? mapGrid)) return default;

            return mapGrid.Grid.TileSize * cellCount * Atmospherics.CellVolume;
        }

        /// <inheritdoc />
        public virtual void Update(float frameTime)
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
                    if (!ProcessTileEqualize(_paused))
                    {
                        _paused = true;
                        return;
                    }

                    _paused = false;
                    _state = ProcessState.ActiveTiles;
                    return;
                case ProcessState.ActiveTiles:
                    if (!ProcessActiveTiles(_paused))
                    {
                        _paused = true;
                        return;
                    }

                    _paused = false;
                    _state = ProcessState.ExcitedGroups;
                    return;
                case ProcessState.ExcitedGroups:
                    if (!ProcessExcitedGroups(_paused))
                    {
                        _paused = true;
                        return;
                    }

                    _paused = false;
                    _state = ProcessState.HighPressureDelta;
                    return;
                case ProcessState.HighPressureDelta:
                    if (!ProcessHighPressureDelta(_paused))
                    {
                        _paused = true;
                        return;
                    }

                    _paused = false;
                    _state = ProcessState.Hotspots;
                    break;
                case ProcessState.Hotspots:
                    if (!ProcessHotspots(_paused))
                    {
                        _paused = true;
                        return;
                    }

                    _paused = false;
                    _state = ProcessState.Superconductivity;
                    break;
                case ProcessState.Superconductivity:
                    if (!ProcessSuperconductivity(_paused))
                    {
                        _paused = true;
                        return;
                    }

                    _paused = false;
                    _state = ProcessState.PipeNet;
                    break;
                case ProcessState.PipeNet:
                    if (!ProcessPipeNets(_paused))
                    {
                        _paused = true;
                        return;
                    }

                    _paused = false;
                    _state = ProcessState.PipeNetDevices;
                    break;
                case ProcessState.PipeNetDevices:
                    if (!ProcessPipeNetDevices(_paused))
                    {
                        _paused = true;
                        return;
                    }

                    _paused = false;
                    _state = ProcessState.TileEqualize;
                    break;
            }

            UpdateCounter++;
        }

        public virtual bool ProcessTileEqualize(bool resumed = false)
        {
            _stopwatch.Restart();

            if(!resumed)
                _currentRunTiles = new Queue<TileAtmosphere>(_activeTiles);

            var number = 0;
            while (_currentRunTiles.Count > 0)
            {
                var tile = _currentRunTiles.Dequeue();
                tile.EqualizePressureInZone(UpdateCounter);

                if (number++ < LagCheckIterations) continue;
                number = 0;
                // Process the rest next time.
                if (_stopwatch.Elapsed.TotalMilliseconds >= LagCheckMaxMilliseconds)
                {
                    _tileEqualizeLastProcess = _stopwatch.Elapsed.TotalMilliseconds;
                    return false;
                }
            }

            _tileEqualizeLastProcess = _stopwatch.Elapsed.TotalMilliseconds;
            return true;
        }

        public virtual bool ProcessActiveTiles(bool resumed = false)
        {
            _stopwatch.Restart();

            if(!resumed)
                _currentRunTiles = new Queue<TileAtmosphere>(_activeTiles);

            var number = 0;
            while (_currentRunTiles.Count > 0)
            {
                var tile = _currentRunTiles.Dequeue();
                tile.ProcessCell(UpdateCounter);

                if (number++ < LagCheckIterations) continue;
                number = 0;
                // Process the rest next time.
                if (_stopwatch.Elapsed.TotalMilliseconds >= LagCheckMaxMilliseconds)
                {
                    _activeTilesLastProcess = _stopwatch.Elapsed.TotalMilliseconds;
                    return false;
                }
            }

            _activeTilesLastProcess = _stopwatch.Elapsed.TotalMilliseconds;
            return true;
        }

        public virtual bool ProcessExcitedGroups(bool resumed = false)
        {
            _stopwatch.Restart();

            if(!resumed)
                _currentRunExcitedGroups = new Queue<ExcitedGroup>(_excitedGroups);

            var number = 0;
            while (_currentRunExcitedGroups.Count > 0)
            {
                var excitedGroup = _currentRunExcitedGroups.Dequeue();
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
                {
                    _excitedGroupLastProcess = _stopwatch.Elapsed.TotalMilliseconds;
                    return false;
                }
            }

            _excitedGroupLastProcess = _stopwatch.Elapsed.TotalMilliseconds;
            return true;
        }

        public virtual bool ProcessHighPressureDelta(bool resumed = false)
        {
            _stopwatch.Restart();

            if(!resumed)
                _currentRunTiles = new Queue<TileAtmosphere>(_highPressureDelta);

            var number = 0;
            while (_currentRunTiles.Count > 0)
            {
                var tile = _currentRunTiles.Dequeue();
                tile.HighPressureMovements();
                tile.PressureDifference = 0f;
                tile.PressureSpecificTarget = null;
                _highPressureDelta.Remove(tile);

                if (number++ < LagCheckIterations) continue;
                number = 0;
                // Process the rest next time.
                if (_stopwatch.Elapsed.TotalMilliseconds >= LagCheckMaxMilliseconds)
                {
                    _highPressureDeltaLastProcess = _stopwatch.Elapsed.TotalMilliseconds;
                    return false;
                }
            }

            _highPressureDeltaLastProcess = _stopwatch.Elapsed.TotalMilliseconds;
            return true;
        }

        protected virtual bool ProcessHotspots(bool resumed = false)
        {
            _stopwatch.Restart();

            if(!resumed)
                _currentRunTiles = new Queue<TileAtmosphere>(_hotspotTiles);

            var number = 0;
            while (_currentRunTiles.Count > 0)
            {
                var hotspot = _currentRunTiles.Dequeue();
                hotspot.ProcessHotspot();

                if (number++ < LagCheckIterations) continue;
                number = 0;
                // Process the rest next time.
                if (_stopwatch.Elapsed.TotalMilliseconds >= LagCheckMaxMilliseconds)
                {
                    _hotspotsLastProcess = _stopwatch.Elapsed.TotalMilliseconds;
                    return false;
                }
            }

            _hotspotsLastProcess = _stopwatch.Elapsed.TotalMilliseconds;
            return true;
        }

        protected virtual bool ProcessSuperconductivity(bool resumed = false)
        {
            _stopwatch.Restart();

            if(!resumed)
                _currentRunTiles = new Queue<TileAtmosphere>(_superconductivityTiles);

            var number = 0;
            while (_currentRunTiles.Count > 0)
            {
                var superconductivity = _currentRunTiles.Dequeue();
                superconductivity.Superconduct();

                if (number++ < LagCheckIterations) continue;
                number = 0;
                // Process the rest next time.
                if (_stopwatch.Elapsed.TotalMilliseconds >= LagCheckMaxMilliseconds)
                {
                    _superconductivityLastProcess = _stopwatch.Elapsed.TotalMilliseconds;
                    return false;
                }
            }

            _superconductivityLastProcess = _stopwatch.Elapsed.TotalMilliseconds;
            return true;
        }

        protected virtual bool ProcessPipeNets(bool resumed = false)
        {
            _stopwatch.Restart();

            if(!resumed)
                _currentRunPipeNet = new Queue<IPipeNet>(_pipeNets);

            var number = 0;
            while (_currentRunPipeNet.Count > 0)
            {
                var pipenet = _currentRunPipeNet.Dequeue();
                pipenet.Update();

                if (number++ < LagCheckIterations) continue;
                number = 0;
                // Process the rest next time.
                if (_stopwatch.Elapsed.TotalMilliseconds >= LagCheckMaxMilliseconds)
                {
                    _pipeNetLastProcess = _stopwatch.Elapsed.TotalMilliseconds;
                    return false;
                }
            }

            _pipeNetLastProcess = _stopwatch.Elapsed.TotalMilliseconds;
            return true;
        }

        protected virtual bool ProcessPipeNetDevices(bool resumed = false)
        {
            _stopwatch.Restart();

            if(!resumed)
                _currentRunPipeNetDevice = new Queue<PipeNetDeviceComponent>(_pipeNetDevices);

            var number = 0;
            while (_currentRunPipeNetDevice.Count > 0)
            {
                var device = _currentRunPipeNetDevice.Dequeue();
                device.Update();

                if (number++ < LagCheckIterations) continue;
                number = 0;
                // Process the rest next time.
                if (_stopwatch.Elapsed.TotalMilliseconds >= LagCheckMaxMilliseconds)
                {
                    _pipeNetDevicesLastProcess = _stopwatch.Elapsed.TotalMilliseconds;
                    return false;
                }
            }

            _pipeNetDevicesLastProcess = _stopwatch.Elapsed.TotalMilliseconds;
            return true;
        }

        protected virtual IEnumerable<AirtightComponent> GetObstructingComponents(Vector2i indices)
        {
            var gridLookup = EntitySystem.Get<GridTileLookupSystem>();

            var list = new List<AirtightComponent>();

            foreach (var v in gridLookup.GetEntitiesIntersecting(_gridId, indices))
            {
                if (v.TryGetComponent<AirtightComponent>(out var ac))
                    list.Add(ac);
            }

            return list;
        }

        private bool NeedsVacuumFixing(Vector2i indices)
        {
            var value = false;

            foreach (var airtightComponent in GetObstructingComponents(indices))
            {
                value |= airtightComponent.FixVacuum;
            }

            return value;
        }

        private AtmosDirection GetBlockedDirections(Vector2i indices)
        {
            var value = AtmosDirection.Invalid;

            foreach (var airtightComponent in GetObstructingComponents(indices))
            {
                if(airtightComponent.AirBlocked)
                    value |= airtightComponent.AirBlockedDirection;
            }

            return value;
        }

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
                    !serializer.TryReadDataField("tiles", out Dictionary<Vector2i, int>? tiles))
                    return;

                Tiles.Clear();

                foreach (var (indices, mix) in tiles!)
                {
                    try
                    {
                        Tiles.Add(indices, new TileAtmosphere(this, gridId, indices, (GasMixture)uniqueMixes![mix].Clone()));
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        Logger.Error($"Error during atmos serialization! Tile at {indices} points to an unique mix ({mix}) out of range!");
                        throw;
                    }

                    Invalidate(indices);
                }
            }
            else if (serializer.Writing)
            {
                var uniqueMixes = new List<GasMixture>();
                var uniqueMixHash = new Dictionary<GasMixture, int>();
                var tiles = new Dictionary<Vector2i, int>();
                foreach (var (indices, tile) in Tiles)
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
                serializer.DataField(ref tiles, "tiles", new Dictionary<Vector2i, int>());
            }
        }

        public IEnumerator<TileAtmosphere> GetEnumerator()
        {
            return Tiles.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        public virtual void BurnTile(Vector2i gridIndices)
        {
            // TODO ATMOS
        }
    }
}
