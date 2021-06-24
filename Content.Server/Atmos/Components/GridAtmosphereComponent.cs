#nullable enable
// ReSharper disable once RedundantUsingDirective
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.CPUJob.JobQueues.Queues;
using Content.Server.NodeContainer.NodeGroups;
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

namespace Content.Server.Atmos.Components
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

        public override string Name => "GridAtmosphere";

        public bool ProcessingPaused { get; set; } = false;
        public float Timer { get; set; }
        private GridId _gridId;

        [ComponentDependency] private IMapGridComponent? _mapGridComponent;

        public virtual bool Simulated => true;

        [ViewVariables]
        public int UpdateCounter { get; set; } = 0;

        [ViewVariables]
        public readonly HashSet<ExcitedGroup> ExcitedGroups = new(1000);

        [ViewVariables]
        public int ExcitedGroupCount => ExcitedGroups.Count;

        [DataField("uniqueMixes")]
        public List<GasMixture>? UniqueMixes;

        [DataField("tiles")]
        public Dictionary<Vector2i, int>? TilesUniqueMixes;

        [ViewVariables]
        public readonly Dictionary<Vector2i, TileAtmosphere> Tiles = new(1000);

        [ViewVariables]
        public readonly HashSet<TileAtmosphere> ActiveTiles = new(1000);

        [ViewVariables]
        public int ActiveTilesCount => ActiveTiles.Count;

        [ViewVariables]
        public readonly HashSet<TileAtmosphere> HotspotTiles = new(1000);

        [ViewVariables]
        public int HotspotTilesCount => HotspotTiles.Count;

        [ViewVariables]
        public readonly HashSet<TileAtmosphere> SuperconductivityTiles = new(1000);

        [ViewVariables]
        public int SuperconductivityTilesCount => SuperconductivityTiles.Count;

        [ViewVariables]
        public readonly HashSet<Vector2i> InvalidatedCoords = new(1000);

        [ViewVariables]
        public HashSet<TileAtmosphere> HighPressureDelta = new(1000);

        [ViewVariables]
        public int HighPressureDeltaCount => HighPressureDelta.Count;

        [ViewVariables]
        public readonly HashSet<IPipeNet> PipeNets = new();

        [ViewVariables]
        public readonly HashSet<AtmosDeviceComponent> AtmosDevices = new();

        [ViewVariables]
        public Queue<TileAtmosphere> CurrentRunTiles = new();

        [ViewVariables]
        public Queue<ExcitedGroup> CurrentRunExcitedGroups = new();

        [ViewVariables]
        public Queue<IPipeNet> CurrentRunPipeNet = new();

        [ViewVariables]
        public Queue<AtmosDeviceComponent> CurrentRunAtmosDevices = new();

        [ViewVariables]
        public AtmosphereProcessingState State { get; set; } = AtmosphereProcessingState.TileEqualize;

        public GridAtmosphereComponent()
        {
            ProcessingPaused = false;
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

            UniqueMixes = uniqueMixes;
            TilesUniqueMixes = tiles;
        }

        protected override void Initialize()
        {
            base.Initialize();

            Tiles.Clear();

            if (TilesUniqueMixes != null && Owner.TryGetComponent(out IMapGridComponent? mapGrid))
            {
                foreach (var (indices, mix) in TilesUniqueMixes)
                {
                    try
                    {
                        Tiles.Add(indices, new TileAtmosphere(this, mapGrid.GridIndex, indices, (GasMixture) UniqueMixes![mix].Clone()));
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

        protected override void OnAdd()
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
                    Tiles.Add(tile.GridIndices, new TileAtmosphere(this, tile.GridIndex, tile.GridIndices, new GasMixture(GetVolumeForCells(1)){Temperature = Atmospherics.T20C}));

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
            InvalidatedCoords.Add(indices);
        }

        public virtual void Revalidate()
        {
            foreach (var indices in InvalidatedCoords)
            {
                var tile = GetTile(indices);

                if (tile == null)
                {
                    tile = new TileAtmosphere(this, _gridId, indices, new GasMixture(GetVolumeForCells(1)){Temperature = Atmospherics.T20C});
                    Tiles[indices] = tile;
                }

                var isAirBlocked = IsAirBlocked(indices);

                if (IsSpace(indices) && !isAirBlocked)
                {
                    tile.Air = new GasMixture(GetVolumeForCells(1));
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

                    tile.Air ??= new GasMixture(GetVolumeForCells(1)){Temperature = Atmospherics.T20C};
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

            InvalidatedCoords.Clear();
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
            tile.Air = new GasMixture(GetVolumeForCells(1)){Temperature = Atmospherics.T20C};
            Tiles[indices] = tile;

            var ratio = 1f / adjacent.Count;

            foreach (var (_, adj) in adjacent)
            {
                var mix = adj.Air!.RemoveRatio(ratio);
                AtmosphereSystem.Merge(tile.Air, mix);
                AtmosphereSystem.Merge(adj.Air, mix);
            }
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void AddActiveTile(TileAtmosphere tile)
        {
            if (tile?.GridIndex != _gridId || tile.Air == null) return;
            tile.Excited = true;
            ActiveTiles.Add(tile);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void RemoveActiveTile(TileAtmosphere tile, bool disposeGroup = true)
        {
            ActiveTiles.Remove(tile);
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
            HotspotTiles.Add(tile);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void RemoveHotspotTile(TileAtmosphere tile)
        {
            HotspotTiles.Remove(tile);
        }

        public virtual void AddSuperconductivityTile(TileAtmosphere tile)
        {
            if (tile?.GridIndex != _gridId || !AtmosphereSystem.Superconduction) return;
            SuperconductivityTiles.Add(tile);
        }

        public virtual void RemoveSuperconductivityTile(TileAtmosphere tile)
        {
            SuperconductivityTiles.Remove(tile);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void AddHighPressureDelta(TileAtmosphere tile)
        {
            if (tile.GridIndex != _gridId) return;
            HighPressureDelta.Add(tile);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool HasHighPressureDelta(TileAtmosphere tile)
        {
            return HighPressureDelta.Contains(tile);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void AddExcitedGroup(ExcitedGroup excitedGroup)
        {
            ExcitedGroups.Add(excitedGroup);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void RemoveExcitedGroup(ExcitedGroup excitedGroup)
        {
            ExcitedGroups.Remove(excitedGroup);
        }

        public virtual void AddPipeNet(IPipeNet pipeNet)
        {
            PipeNets.Add(pipeNet);
        }

        public virtual void RemovePipeNet(IPipeNet pipeNet)
        {
            PipeNets.Remove(pipeNet);
        }

        public virtual void AddAtmosDevice(AtmosDeviceComponent atmosDevice)
        {
            AtmosDevices.Add(atmosDevice);
        }

        public virtual void RemoveAtmosDevice(AtmosDeviceComponent atmosDevice)
        {
            AtmosDevices.Remove(atmosDevice);
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
                return new TileAtmosphere(this, _gridId, indices, new GasMixture(GetVolumeForCells(1)){Temperature = Atmospherics.TCMB}, true);
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
