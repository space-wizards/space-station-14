#nullable enable
// ReSharper disable once RedundantUsingDirective
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
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;
using Dependency = Robust.Shared.IoC.DependencyAttribute;

namespace Content.Server.GameObjects.Components.Atmos
{
    /// <summary>
    ///     This is our SSAir equivalent.
    /// </summary>
    [ComponentReference(typeof(IGridAtmosphereComponent))]
    [RegisterComponent, Serializable]
    public class GridAtmosphereComponent : Component, IGridAtmosphereComponent, ISerializationHooks
    {
        [Dependency] private IMapManager _mapManager = default!;
        [Dependency] private ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private IServerEntityManager _serverEntityManager = default!;
        [Dependency] private IGameTiming _gameTiming = default!;

        public GridTileLookupSystem GridTileLookupSystem { get; private set; } = default!;
        internal GasTileOverlaySystem GasTileOverlaySystem { get; private set; } = default!;
        public AtmosphereSystem AtmosphereSystem { get; private set; } = default!;

        /// <summary>
        ///     Check current execution time every n instances processed.
        /// </summary>
        private const int LagCheckIterations = 30;

        public override string Name => "GridAtmosphere";

        private bool _paused;
        private float _timer;
        private Stopwatch _stopwatch = new();
        private GridId _gridId;

        [ComponentDependency] private IMapGridComponent? _mapGridComponent;

        public virtual bool Simulated => true;

        [ViewVariables]
        public int UpdateCounter { get; private set; } = 0;

        [ViewVariables]
        private double _tileEqualizeLastProcess;

        [ViewVariables]
        private readonly HashSet<ExcitedGroup> _excitedGroups = new(1000);

        [ViewVariables]
        private int ExcitedGroupCount => _excitedGroups.Count;

        [ViewVariables]
        private double _excitedGroupLastProcess;

        [DataField("uniqueMixes")]
        private List<GasMixture>? _uniqueMixes;

        [DataField("tiles")]
        private Dictionary<Vector2i, int>? _tiles;

        [ViewVariables]
        protected readonly Dictionary<Vector2i, TileAtmosphere> Tiles = new(1000);

        [ViewVariables]
        private readonly HashSet<TileAtmosphere> _activeTiles = new(1000);

        [ViewVariables]
        private int ActiveTilesCount => _activeTiles.Count;

        [ViewVariables]
        private double _activeTilesLastProcess;

        [ViewVariables]
        private readonly HashSet<TileAtmosphere> _hotspotTiles = new(1000);

        [ViewVariables]
        private int HotspotTilesCount => _hotspotTiles.Count;

        [ViewVariables]
        private double _hotspotsLastProcess;

        [ViewVariables]
        private readonly HashSet<TileAtmosphere> _superconductivityTiles = new(1000);

        [ViewVariables]
        private int SuperconductivityTilesCount => _superconductivityTiles.Count;

        [ViewVariables]
        private double _superconductivityLastProcess;

        [ViewVariables]
        private readonly HashSet<Vector2i> _invalidatedCoords = new(1000);

        [ViewVariables]
        private int InvalidatedCoordsCount => _invalidatedCoords.Count;

        [ViewVariables]
        private HashSet<TileAtmosphere> _highPressureDelta = new(1000);

        [ViewVariables]
        private int HighPressureDeltaCount => _highPressureDelta.Count;

        [ViewVariables]
        private double _highPressureDeltaLastProcess;

        [ViewVariables]
        private readonly HashSet<IPipeNet> _pipeNets = new();

        [ViewVariables]
        private double _pipeNetLastProcess;

        [ViewVariables]
        private readonly HashSet<AtmosDeviceComponent> _atmosDevices = new();

        [ViewVariables]
        private double _atmosDevicesLastProcess;

        [ViewVariables]
        private Queue<TileAtmosphere> _currentRunTiles = new();

        [ViewVariables]
        private Queue<ExcitedGroup> _currentRunExcitedGroups = new();

        [ViewVariables]
        private Queue<IPipeNet> _currentRunPipeNet = new();

        [ViewVariables]
        private Queue<AtmosDeviceComponent> _currentRunAtmosDevices = new();

        [ViewVariables]
        private ProcessState _state = ProcessState.TileEqualize;

        public GridAtmosphereComponent()
        {
            _paused = false;
        }

        private enum ProcessState
        {
            TileEqualize,
            ActiveTiles,
            ExcitedGroups,
            HighPressureDelta,
            Hotspots,
            Superconductivity,
            PipeNet,
            AtmosDevices,
        }

        /// <inheritdoc />
        public virtual void PryTile(Vector2i indices)
        {
            if (IsSpace(indices) || IsAirBlocked(indices)) return;

            indices.PryTile(_gridId, _mapManager, _tileDefinitionManager, _serverEntityManager);
        }

        void ISerializationHooks.BeforeSerialization()
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

            if (uniqueMixes.Count == 0) uniqueMixes = null;
            if (tiles.Count == 0) tiles = null;

            _uniqueMixes = uniqueMixes;
            _tiles = tiles;
        }

        public override void Initialize()
        {
            base.Initialize();

            Tiles.Clear();

            if (_tiles != null && Owner.TryGetComponent(out IMapGridComponent? mapGrid))
            {
                foreach (var (indices, mix) in _tiles)
                {
                    try
                    {
                        Tiles.Add(indices, new TileAtmosphere(this, mapGrid.GridIndex, indices, (GasMixture) _uniqueMixes![mix].Clone()));
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        Logger.Error($"Error during atmos serialization! Tile at {indices} points to an unique mix ({mix}) out of range!");
                        throw;
                    }

                    Invalidate(indices);
                }
            }

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
            foreach (var indices in _invalidatedCoords)
            {
                var tile = GetTile(indices);

                if (tile == null)
                {
                    tile = new TileAtmosphere(this, _gridId, indices, new GasMixture(GetVolumeForCells(1), AtmosphereSystem){Temperature = Atmospherics.T20C});
                    Tiles[indices] = tile;
                }

                var isAirBlocked = IsAirBlocked(indices);

                if (IsSpace(indices) && !isAirBlocked)
                {
                    tile.Air = new GasMixture(GetVolumeForCells(1), AtmosphereSystem);
                    tile.Air.MarkImmutable();
                    Tiles[indices] = tile;

                } else if (isAirBlocked)
                {
                    var nullAir = false;

                    foreach (var airtight in GetObstructingComponents(indices))
                    {
                        if (airtight.NoAirWhenFullyAirBlocked)
                        {
                            nullAir = true;
                            break;
                        }
                    }

                    if(nullAir)
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

                // By removing the active tile, we effectively remove its excited group, if any.
                RemoveActiveTile(tile);

                // Then we activate the tile again.
                AddActiveTile(tile);

                tile.BlockedAirflow = GetBlockedDirections(indices);

                // TODO ATMOS: Query all the contents of this tile (like walls) and calculate the correct thermal conductivity
                tile.ThermalConductivity = tile.Tile?.Tile.GetContentTileDefinition().ThermalConductivity ?? 0.5f;
                tile.UpdateAdjacent();
                GasTileOverlaySystem.Invalidate(_gridId, indices);

                for (var i = 0; i < Atmospherics.Directions; i++)
                {
                    var direction = (AtmosDirection) (1 << i);
                    var otherIndices = indices.Offset(direction.ToDirection());
                    var otherTile = GetTile(otherIndices);
                    if (otherTile != null) AddActiveTile(otherTile);
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
            // includeAirBlocked is false, therefore all tiles in this have Air != null.
            var adjacent = GetAdjacentTiles(indices);
            tile.Air = new GasMixture(GetVolumeForCells(1), AtmosphereSystem){Temperature = Atmospherics.T20C};
            Tiles[indices] = tile;

            var ratio = 1f / adjacent.Count;

            foreach (var (_, adj) in adjacent)
            {
                var mix = adj.Air!.RemoveRatio(ratio);
                tile.Air.Merge(mix);
                adj.Air.Merge(mix);
            }
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void AddActiveTile(TileAtmosphere tile)
        {
            if (tile?.GridIndex != _gridId || tile.Air == null) return;
            tile.Excited = true;
            _activeTiles.Add(tile);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void RemoveActiveTile(TileAtmosphere tile, bool disposeGroup = true)
        {
            _activeTiles.Remove(tile);
            tile.Excited = false;
            if(disposeGroup)
                tile.ExcitedGroup?.Dispose();
            else
                tile.ExcitedGroup?.RemoveTile(tile);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void AddHotspotTile(TileAtmosphere tile)
        {
            if (tile?.GridIndex != _gridId || tile?.Air == null) return;
            _hotspotTiles.Add(tile);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void RemoveHotspotTile(TileAtmosphere tile)
        {
            _hotspotTiles.Remove(tile);
        }

        public virtual void AddSuperconductivityTile(TileAtmosphere tile)
        {
            if (tile?.GridIndex != _gridId || !AtmosphereSystem.Superconduction) return;
            _superconductivityTiles.Add(tile);
        }

        public virtual void RemoveSuperconductivityTile(TileAtmosphere tile)
        {
            _superconductivityTiles.Remove(tile);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void AddHighPressureDelta(TileAtmosphere tile)
        {
            if (tile.GridIndex != _gridId) return;
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

        public virtual void AddAtmosDevice(AtmosDeviceComponent atmosDevice)
        {
            _atmosDevices.Add(atmosDevice);
        }

        public virtual void RemoveAtmosDevice(AtmosDeviceComponent atmosDevice)
        {
            _atmosDevices.Remove(atmosDevice);
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
            var directions = AtmosDirection.Invalid;

            foreach (var obstructingComponent in GetObstructingComponents(indices))
            {
                if (!obstructingComponent.AirBlocked)
                    continue;

                // We set the directions that are air-blocked so far,
                // as you could have a full obstruction with only 4 directional air blockers.
                directions |= obstructingComponent.AirBlockedDirection;

                if (directions.IsFlagSet(direction))
                    return true;
            }

            return false;
        }

        /// <inheritdoc />
        public virtual bool IsSpace(Vector2i indices)
        {
            if (_mapGridComponent == null) return default;

            return _mapGridComponent.Grid.GetTileRef(indices).IsSpace();
        }

        public Dictionary<AtmosDirection, TileAtmosphere> GetAdjacentTiles(EntityCoordinates coordinates, bool includeAirBlocked = false)
        {
            return GetAdjacentTiles(coordinates.ToVector2i(_serverEntityManager, _mapManager), includeAirBlocked);
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
            if (_mapGridComponent == null) return default;

            return _mapGridComponent.Grid.TileSize * cellCount * Atmospherics.CellVolume;
        }

        /// <inheritdoc />
        public virtual void Update(float frameTime)
        {
            _timer += frameTime;
            var atmosTime = 1f/AtmosphereSystem.AtmosTickRate;

            if (_invalidatedCoords.Count != 0)
                Revalidate();

            if (_timer < atmosTime)
                return;

            // We subtract it so it takes lost time into account.
            _timer -= atmosTime;

            var maxProcessTime = AtmosphereSystem.AtmosMaxProcessTime;

            switch (_state)
            {
                case ProcessState.TileEqualize:
                    if (!ProcessTileEqualize(_paused, maxProcessTime))
                    {
                        _paused = true;
                        return;
                    }

                    _paused = false;
                    _state = ProcessState.ActiveTiles;
                    return;
                case ProcessState.ActiveTiles:
                    if (!ProcessActiveTiles(_paused, maxProcessTime))
                    {
                        _paused = true;
                        return;
                    }

                    _paused = false;
                    _state = ProcessState.ExcitedGroups;
                    return;
                case ProcessState.ExcitedGroups:
                    if (!ProcessExcitedGroups(_paused, maxProcessTime))
                    {
                        _paused = true;
                        return;
                    }

                    _paused = false;
                    _state = ProcessState.HighPressureDelta;
                    return;
                case ProcessState.HighPressureDelta:
                    if (!ProcessHighPressureDelta(_paused, maxProcessTime))
                    {
                        _paused = true;
                        return;
                    }

                    _paused = false;
                    _state = ProcessState.Hotspots;
                    break;
                case ProcessState.Hotspots:
                    if (!ProcessHotspots(_paused, maxProcessTime))
                    {
                        _paused = true;
                        return;
                    }

                    _paused = false;
                    // Next state depends on whether superconduction is enabled or not.
                    // Note: We do this here instead of on the tile equalization step to prevent ending it early.
                    //       Therefore, a change to this CVar might only be applied after that step is over.
                    _state = AtmosphereSystem.Superconduction ? ProcessState.Superconductivity : ProcessState.PipeNet;
                    break;
                case ProcessState.Superconductivity:
                    if (!ProcessSuperconductivity(_paused, maxProcessTime))
                    {
                        _paused = true;
                        return;
                    }

                    _paused = false;
                    _state = ProcessState.PipeNet;
                    break;
                case ProcessState.PipeNet:
                    if (!ProcessPipeNets(_paused, maxProcessTime))
                    {
                        _paused = true;
                        return;
                    }

                    _paused = false;
                    _state = ProcessState.AtmosDevices;
                    break;
                case ProcessState.AtmosDevices:
                    if (!ProcessAtmosDevices(_paused, maxProcessTime))
                    {
                        _paused = true;
                        return;
                    }

                    _paused = false;
                    // Next state depends on whether monstermos equalization is enabled or not.
                    // Note: We do this here instead of on the tile equalization step to prevent ending it early.
                    //       Therefore, a change to this CVar might only be applied after that step is over.
                    _state = AtmosphereSystem.MonstermosEqualization ? ProcessState.TileEqualize : ProcessState.ActiveTiles;
                    break;
            }

            UpdateCounter++;
        }

        public virtual bool ProcessTileEqualize(bool resumed = false, float lagCheck = 5f)
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
                if (_stopwatch.Elapsed.TotalMilliseconds >= lagCheck)
                {
                    _tileEqualizeLastProcess = _stopwatch.Elapsed.TotalMilliseconds;
                    return false;
                }
            }

            _tileEqualizeLastProcess = _stopwatch.Elapsed.TotalMilliseconds;
            return true;
        }

        public virtual bool ProcessActiveTiles(bool resumed = false, float lagCheck = 5f)
        {
            _stopwatch.Restart();

            var spaceWind = AtmosphereSystem.SpaceWind;

            if(!resumed)
                _currentRunTiles = new Queue<TileAtmosphere>(_activeTiles);

            var number = 0;
            while (_currentRunTiles.Count > 0)
            {
                var tile = _currentRunTiles.Dequeue();
                tile.ProcessCell(UpdateCounter, spaceWind);

                if (number++ < LagCheckIterations) continue;
                number = 0;
                // Process the rest next time.
                if (_stopwatch.Elapsed.TotalMilliseconds >= lagCheck)
                {
                    _activeTilesLastProcess = _stopwatch.Elapsed.TotalMilliseconds;
                    return false;
                }
            }

            _activeTilesLastProcess = _stopwatch.Elapsed.TotalMilliseconds;
            return true;
        }

        public virtual bool ProcessExcitedGroups(bool resumed = false, float lagCheck = 5f)
        {
            _stopwatch.Restart();

            var spaceIsAllConsuming = AtmosphereSystem.ExcitedGroupsSpaceIsAllConsuming;

            if(!resumed)
                _currentRunExcitedGroups = new Queue<ExcitedGroup>(_excitedGroups);

            var number = 0;
            while (_currentRunExcitedGroups.Count > 0)
            {
                var excitedGroup = _currentRunExcitedGroups.Dequeue();
                excitedGroup.BreakdownCooldown++;
                excitedGroup.DismantleCooldown++;

                if(excitedGroup.BreakdownCooldown > Atmospherics.ExcitedGroupBreakdownCycles)
                    excitedGroup.SelfBreakdown(spaceIsAllConsuming);

                else if(excitedGroup.DismantleCooldown > Atmospherics.ExcitedGroupsDismantleCycles)
                    excitedGroup.Dismantle();

                if (number++ < LagCheckIterations) continue;
                number = 0;
                // Process the rest next time.
                if (_stopwatch.Elapsed.TotalMilliseconds >= lagCheck)
                {
                    _excitedGroupLastProcess = _stopwatch.Elapsed.TotalMilliseconds;
                    return false;
                }
            }

            _excitedGroupLastProcess = _stopwatch.Elapsed.TotalMilliseconds;
            return true;
        }

        public virtual bool ProcessHighPressureDelta(bool resumed = false, float lagCheck = 5f)
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
                if (_stopwatch.Elapsed.TotalMilliseconds >= lagCheck)
                {
                    _highPressureDeltaLastProcess = _stopwatch.Elapsed.TotalMilliseconds;
                    return false;
                }
            }

            _highPressureDeltaLastProcess = _stopwatch.Elapsed.TotalMilliseconds;
            return true;
        }

        protected virtual bool ProcessHotspots(bool resumed = false, float lagCheck = 5f)
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
                if (_stopwatch.Elapsed.TotalMilliseconds >= lagCheck)
                {
                    _hotspotsLastProcess = _stopwatch.Elapsed.TotalMilliseconds;
                    return false;
                }
            }

            _hotspotsLastProcess = _stopwatch.Elapsed.TotalMilliseconds;
            return true;
        }

        protected virtual bool ProcessSuperconductivity(bool resumed = false, float lagCheck = 5f)
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
                if (_stopwatch.Elapsed.TotalMilliseconds >= lagCheck)
                {
                    _superconductivityLastProcess = _stopwatch.Elapsed.TotalMilliseconds;
                    return false;
                }
            }

            _superconductivityLastProcess = _stopwatch.Elapsed.TotalMilliseconds;
            return true;
        }

        protected virtual bool ProcessPipeNets(bool resumed = false, float lagCheck = 5f)
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
                if (_stopwatch.Elapsed.TotalMilliseconds >= lagCheck)
                {
                    _pipeNetLastProcess = _stopwatch.Elapsed.TotalMilliseconds;
                    return false;
                }
            }

            _pipeNetLastProcess = _stopwatch.Elapsed.TotalMilliseconds;
            return true;
        }

        protected virtual bool ProcessAtmosDevices(bool resumed = false, float lagCheck = 5f)
        {
            _stopwatch.Restart();

            if(!resumed)
                _currentRunAtmosDevices = new Queue<AtmosDeviceComponent>(_atmosDevices);

            var number = 0;
            while (_currentRunAtmosDevices.Count > 0)
            {
                var device = _currentRunAtmosDevices.Dequeue();
                device.Update(_gameTiming);

                if (number++ < LagCheckIterations) continue;
                number = 0;
                // Process the rest next time.
                if (_stopwatch.Elapsed.TotalMilliseconds >= lagCheck)
                {
                    _atmosDevicesLastProcess = _stopwatch.Elapsed.TotalMilliseconds;
                    return false;
                }
            }

            _atmosDevicesLastProcess = _stopwatch.Elapsed.TotalMilliseconds;
            return true;
        }

        protected virtual IEnumerable<AirtightComponent> GetObstructingComponents(Vector2i indices)
        {
            var gridLookup = EntitySystem.Get<GridTileLookupSystem>();

            foreach (var v in gridLookup.GetEntitiesIntersecting(_gridId, indices))
            {
                if (v.TryGetComponent<AirtightComponent>(out var ac))
                    yield return ac;
            }
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
