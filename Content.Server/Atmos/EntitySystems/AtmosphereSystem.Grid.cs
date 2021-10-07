using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Reactions;
using Content.Server.NodeContainer.NodeGroups;
using Content.Shared.Atmos;
using Content.Shared.Maps;
using Robust.Shared.GameObjects;
// ReSharper disable once RedundantUsingDirective
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Dependency = Robust.Shared.IoC.DependencyAttribute;

namespace Content.Server.Atmos.EntitySystems
{
    public partial class AtmosphereSystem
    {
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly GasTileOverlaySystem _gasTileOverlaySystem = default!;

        private void InitializeGrid()
        {
            SubscribeLocalEvent<GridAtmosphereComponent, ComponentInit>(OnGridAtmosphereInit);
        }

        private void OnGridAtmosphereInit(EntityUid uid, GridAtmosphereComponent gridAtmosphere, ComponentInit args)
        {
            base.Initialize();

            gridAtmosphere.Tiles.Clear();

            if (!EntityManager.TryGetComponent(uid, out IMapGridComponent? mapGrid))
                return;

            if (gridAtmosphere.TilesUniqueMixes != null)
            {
                foreach (var (indices, mix) in gridAtmosphere.TilesUniqueMixes)
                {
                    try
                    {
                        gridAtmosphere.Tiles.Add(indices, new TileAtmosphere(mapGrid.GridIndex, indices, (GasMixture) gridAtmosphere.UniqueMixes![mix].Clone()));
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        Logger.Error($"Error during atmos serialization! Tile at {indices} points to an unique mix ({mix}) out of range!");
                        throw;
                    }

                    InvalidateTile(gridAtmosphere, indices);
                }
            }

            GridRepopulateTiles(mapGrid.Grid, gridAtmosphere);
        }

        #region Grid Is Simulated

        /// <summary>
        ///     Returns whether a grid has a simulated atmosphere.
        /// </summary>
        /// <param name="coordinates">Coordinates to be checked.</param>
        /// <returns>Whether the grid has a simulated atmosphere.</returns>
        public bool IsSimulatedGrid(EntityCoordinates coordinates)
        {
            if (TryGetGridAndTile(coordinates, out var tuple))
                return IsSimulatedGrid(tuple.Value.Grid);

            return false;
        }

        /// <summary>
        ///     Returns whether a grid has a simulated atmosphere.
        /// </summary>
        /// <param name="grid">Grid to be checked.</param>
        /// <returns>Whether the grid has a simulated atmosphere.</returns>
        public bool IsSimulatedGrid(GridId grid)
        {
            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return false;

            if (EntityManager.HasComponent<GridAtmosphereComponent>(mapGrid.GridEntityId))
                return true;

            return false;
        }

        #endregion

        #region Grid Get All Mixtures

        /// <summary>
        ///     Gets all tile mixtures within a grid atmosphere, optionally invalidating them all.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the grid to get all tile mixtures from.</param>
        /// <param name="invalidate">Whether to invalidate all tiles.</param>
        /// <returns>All tile mixtures in a grid.</returns>
        public IEnumerable<GasMixture> GetAllTileMixtures(EntityCoordinates coordinates, bool invalidate = false)
        {
            if (TryGetGridAndTile(coordinates, out var tuple))
                return GetAllTileMixtures(tuple.Value.Grid, invalidate);

            return Enumerable.Empty<GasMixture>();
        }

        /// <summary>
        ///     Gets all tile mixtures within a grid atmosphere, optionally invalidating them all.
        /// </summary>
        /// <param name="grid">Grid where to get all tile mixtures from.</param>
        /// <param name="invalidate">Whether to invalidate all tiles.</param>
        /// <returns>All tile mixtures in a grid.</returns>
        public IEnumerable<GasMixture> GetAllTileMixtures(GridId grid, bool invalidate = false)
        {
            // Return an array with a single space gas mixture for invalid grids.
            if (!grid.IsValid())
                return new []{ GasMixture.SpaceGas };

            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return Enumerable.Empty<GasMixture>();

            if (EntityManager.TryGetComponent(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                return GetAllTileMixtures(gridAtmosphere, invalidate);
            }

            return Enumerable.Empty<GasMixture>();
        }

        /// <summary>
        ///     Gets all tile mixtures within a grid atmosphere, optionally invalidating them all.
        /// </summary>
        /// <param name="gridAtmosphere">Grid Atmosphere to get all mixtures from.</param>
        /// <param name="invalidate">Whether to invalidate all mixtures.</param>
        /// <returns>All the tile mixtures in a grid.</returns>
        public IEnumerable<GasMixture> GetAllTileMixtures(GridAtmosphereComponent gridAtmosphere, bool invalidate = false)
        {
            foreach (var (indices, tile) in gridAtmosphere.Tiles)
            {
                if (tile.Air == null)
                    continue;

                if (invalidate)
                    InvalidateTile(gridAtmosphere, indices);

                yield return tile.Air;
            }
        }

        #endregion

        #region Grid Cell Volume

        /// <summary>
        ///     Gets the volume in liters for a number of tiles, on a specific grid.
        /// </summary>
        /// <param name="grid">The grid in question.</param>
        /// <param name="tiles">The amount of tiles.</param>
        /// <returns>The volume in liters that the tiles occupy.</returns>
        public float GetVolumeForTiles(GridId grid, int tiles = 1)
        {
            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return Atmospherics.CellVolume * tiles;

            return GetVolumeForTiles(mapGrid, tiles);

        }

        /// <summary>
        ///     Gets the volume in liters for a number of tiles, on a specific grid.
        /// </summary>
        /// <param name="mapGrid">The grid in question.</param>
        /// <param name="tiles">The amount of tiles.</param>
        /// <returns>The volume in liters that the tiles occupy.</returns>
        public float GetVolumeForTiles(IMapGrid mapGrid, int tiles = 1)
        {
            return Atmospherics.CellVolume * mapGrid.TileSize * tiles;

        }

        #endregion

        #region Grid Get Obstructing

        /// <summary>
        ///     Gets all obstructing AirtightComponent instances in a specific tile.
        /// </summary>
        /// <param name="mapGrid">The grid where to get the tile.</param>
        /// <param name="tile">The indices of the tile.</param>
        /// <returns></returns>
        public virtual IEnumerable<AirtightComponent> GetObstructingComponents(IMapGrid mapGrid, Vector2i tile)
        {
            foreach (var uid in mapGrid.GetAnchoredEntities(tile))
            {
                if (EntityManager.TryGetComponent<AirtightComponent>(uid, out var ac))
                    yield return ac;
            }
        }

        private AtmosDirection GetBlockedDirections(IMapGrid mapGrid, Vector2i indices)
        {
            var value = AtmosDirection.Invalid;

            foreach (var airtightComponent in GetObstructingComponents(mapGrid, indices))
            {
                if(airtightComponent.AirBlocked)
                    value |= airtightComponent.AirBlockedDirection;
            }

            return value;
        }

        #endregion

        #region Grid Revalidate

        /// <summary>
        ///     Revalidates all invalid coordinates in a grid atmosphere.
        /// </summary>
        /// <param name="mapGrid">The grid in question.</param>
        /// <param name="gridAtmosphere">The grid atmosphere in question.</param>
        /// <returns>Whether the process succeeded or got paused due to time constrains.</returns>
        private bool GridRevalidate(IMapGrid mapGrid, GridAtmosphereComponent gridAtmosphere)
        {
            var volume = GetVolumeForTiles(mapGrid, 1);

            if (!gridAtmosphere.RevalidatePaused)
                gridAtmosphere.CurrentRunInvalidatedCoordinates = new Queue<Vector2i>(gridAtmosphere.InvalidatedCoords);

            gridAtmosphere.InvalidatedCoords.Clear();

            var number = 0;
            while (gridAtmosphere.CurrentRunInvalidatedCoordinates.TryDequeue(out var indices))
            {
                var tile = GetTileAtmosphere(gridAtmosphere, indices);

                if (tile == null)
                {
                    tile = new TileAtmosphere(mapGrid.Index, indices, new GasMixture(volume){Temperature = Atmospherics.T20C});
                    gridAtmosphere.Tiles[indices] = tile;
                }

                var isAirBlocked = IsTileAirBlocked(mapGrid, indices);

                tile.BlockedAirflow = GetBlockedDirections(mapGrid, indices);
                UpdateAdjacent(mapGrid, gridAtmosphere, tile);

                if (IsTileSpace(mapGrid, indices) && !isAirBlocked)
                {
                    tile.Air = new GasMixture(volume);
                    tile.Air.MarkImmutable();
                    gridAtmosphere.Tiles[indices] = tile;

                } else if (isAirBlocked)
                {
                    var nullAir = false;

                    foreach (var airtight in GetObstructingComponents(mapGrid, indices))
                    {
                        if (!airtight.NoAirWhenFullyAirBlocked)
                            continue;

                        nullAir = true;
                        break;
                    }

                    if (nullAir)
                    {
                        tile.Air = null;
                        tile.Hotspot = new Hotspot();
                    }
                }
                else
                {
                    if (tile.Air == null && NeedsVacuumFixing(mapGrid, indices))
                    {
                        FixVacuum(gridAtmosphere, tile.GridIndices);
                    }

                    // Tile used to be space, but isn't anymore.
                    if (tile.Air?.Immutable ?? false)
                    {
                        tile.Air = null;
                    }

                    tile.Air ??= new GasMixture(volume){Temperature = Atmospherics.T20C};
                }

                // By removing the active tile, we effectively remove its excited group, if any.
                RemoveActiveTile(gridAtmosphere, tile);

                // Then we activate the tile again.
                AddActiveTile(gridAtmosphere, tile);

                // TODO ATMOS: Query all the contents of this tile (like walls) and calculate the correct thermal conductivity
                tile.ThermalConductivity = tile.Tile?.Tile.GetContentTileDefinition().ThermalConductivity ?? 0.5f;
                InvalidateVisuals(mapGrid.Index, indices);

                for (var i = 0; i < Atmospherics.Directions; i++)
                {
                    var direction = (AtmosDirection) (1 << i);
                    var otherIndices = indices.Offset(direction);
                    var otherTile = GetTileAtmosphereOrCreateSpace(mapGrid, gridAtmosphere, otherIndices);
                    if (otherTile != null)
                        AddActiveTile(gridAtmosphere, otherTile);
                }

                if (number++ < InvalidCoordinatesLagCheckIterations) continue;
                number = 0;
                // Process the rest next time.
                if (_simulationStopwatch.Elapsed.TotalMilliseconds >= AtmosMaxProcessTime)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Grid Repopulate

        /// <summary>
        ///     Repopulates all tiles on a grid atmosphere.
        /// </summary>
        /// <param name="mapGrid">The grid where to get all valid tiles from.</param>
        /// <param name="gridAtmosphere">The grid atmosphere where the tiles will be repopulated.</param>
        public void GridRepopulateTiles(IMapGrid mapGrid, GridAtmosphereComponent gridAtmosphere)
        {
            var volume = GetVolumeForTiles(mapGrid, 1);

            foreach (var tile in mapGrid.GetAllTiles())
            {
                if(!gridAtmosphere.Tiles.ContainsKey(tile.GridIndices))
                    gridAtmosphere.Tiles[tile.GridIndices] = new TileAtmosphere(tile.GridIndex, tile.GridIndices, new GasMixture(volume){Temperature = Atmospherics.T20C});

                InvalidateTile(gridAtmosphere, tile.GridIndices);
            }

            foreach (var (position, tile) in gridAtmosphere.Tiles.ToArray())
            {
                UpdateAdjacent(mapGrid, gridAtmosphere, tile);
                InvalidateVisuals(mapGrid.Index, position);
            }
        }

        #endregion

        #region Tile Pry

        /// <summary>
        ///     Pries a tile in a grid.
        /// </summary>
        /// <param name="mapGrid">The grid in question.</param>
        /// <param name="tile">The indices of the tile.</param>
        private void PryTile(IMapGrid mapGrid, Vector2i tile)
        {
            if (!mapGrid.TryGetTileRef(tile, out var tileRef))
                return;

            tileRef.PryTile(_mapManager, _tileDefinitionManager, EntityManager);
        }

        #endregion

        #region Tile Invalidate

        /// <summary>
        ///     Invalidates a tile at a certain position.
        /// </summary>
        /// <param name="coordinates">Coordinates of the tile.</param>
        public void InvalidateTile(EntityCoordinates coordinates)
        {
            if(TryGetGridAndTile(coordinates, out var tuple))
                InvalidateTile(tuple.Value.Grid, tuple.Value.Tile);
        }

        /// <summary>
        ///     Invalidates a tile at a certain position.
        /// </summary>
        /// <param name="grid">Grid where to invalidate the tile.</param>
        /// <param name="tile">The indices of the tile.</param>
        public void InvalidateTile(GridId grid, Vector2i tile)
        {
            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return;

            if (EntityManager.TryGetComponent(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                InvalidateTile(gridAtmosphere, tile);
                return;
            }
        }

        /// <summary>
        ///     Invalidates a tile at a certain position.
        /// </summary>
        /// <param name="gridAtmosphere">Grid Atmosphere where to invalidate the tile.</param>
        /// <param name="tile">The tile's indices.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InvalidateTile(GridAtmosphereComponent gridAtmosphere, Vector2i tile)
        {
            gridAtmosphere.InvalidatedCoords.Add(tile);
        }

        #endregion

        #region Tile Invalidate Visuals

        public void InvalidateVisuals(EntityCoordinates coordinates)
        {
            if(TryGetGridAndTile(coordinates, out var tuple))
                InvalidateVisuals(tuple.Value.Grid, tuple.Value.Tile);
        }

        public void InvalidateVisuals(GridId grid, Vector2i tile)
        {
            _gasTileOverlaySystem.Invalidate(grid, tile);
        }

        #endregion

        #region Tile Atmosphere Get

        /// <summary>
        ///     Gets the tile atmosphere in a position, or null.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        /// <remarks>Do NOT use this outside of atmos internals.</remarks>
        /// <returns>The Tile Atmosphere in the position, or null if not on a grid.</returns>
        public TileAtmosphere? GetTileAtmosphere(EntityCoordinates coordinates)
        {
            if (TryGetGridAndTile(coordinates, out var tuple))
                return GetTileAtmosphere(tuple.Value.Grid, tuple.Value.Tile);

            return null;
        }

        /// <summary>
        ///     Gets the tile atmosphere in a position, or null.
        /// </summary>
        /// <param name="grid">Grid where to get the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        /// <remarks>Do NOT use this outside of atmos internals.</remarks>
        /// <returns>The Tile Atmosphere in the position, or null.</returns>
        public TileAtmosphere? GetTileAtmosphere(GridId grid, Vector2i tile)
        {
            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return null;

            if(EntityManager.TryGetComponent(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                return GetTileAtmosphere(gridAtmosphere, tile);
            }

            return null;
        }

        /// <summary>
        ///     Gets the tile atmosphere in a position, or null.
        /// </summary>
        /// <param name="gridAtmosphere">Grid atmosphere where to get the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        /// <remarks>Do NOT use this outside of atmos internals.</remarks>
        /// <returns>The Tile Atmosphere in the position, or null.</returns>
        public TileAtmosphere? GetTileAtmosphere(GridAtmosphereComponent gridAtmosphere, Vector2i tile)
        {
            if (gridAtmosphere.Tiles.TryGetValue(tile, out var tileAtmosphere))
                return tileAtmosphere;

            return null;
        }

        /// <summary>
        ///     Gets the tile atmosphere in a position and if not possible returns a space tile or null.
        /// </summary>
        /// <param name="coordinates">Coordinates of the tile.</param>
        /// <remarks>Do NOT use this outside of atmos internals.</remarks>
        /// <returns>The tile atmosphere of a specific position in a grid, a space tile atmosphere if the tile is space or null if not on a grid.</returns>
        public TileAtmosphere? GetTileAtmosphereOrCreateSpace(EntityCoordinates coordinates)
        {
            if (TryGetGridAndTile(coordinates, out var tuple))
                return GetTileAtmosphereOrCreateSpace(tuple.Value.Grid, tuple.Value.Tile);

            return null;
        }

        /// <summary>
        ///     Gets the tile atmosphere in a position and if not possible returns a space tile or null.
        /// </summary>
        /// <param name="grid">Grid where to get the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        /// <remarks>Do NOT use this outside of atmos internals.</remarks>
        /// <returns>The tile atmosphere of a specific position in a grid, a space tile atmosphere if the tile is space or null if the grid doesn't exist.</returns>
        public TileAtmosphere? GetTileAtmosphereOrCreateSpace(GridId grid, Vector2i tile)
        {
            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return null;

            if (EntityManager.TryGetComponent(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                return GetTileAtmosphereOrCreateSpace(mapGrid, gridAtmosphere, tile);
            }

            return null;
        }

        /// <summary>
        ///     Gets the tile atmosphere in a position and if not possible returns a space tile or null.
        /// </summary>
        /// <param name="mapGrid">Grid where to get the tile.</param>
        /// <param name="gridAtmosphere">Grid Atmosphere where to get the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        /// <remarks>Do NOT use this outside of atmos internals.</remarks>
        /// <returns>The tile atmosphere of a specific position in a grid or a space tile atmosphere if the tile is space.</returns>
        public TileAtmosphere GetTileAtmosphereOrCreateSpace(IMapGrid mapGrid, GridAtmosphereComponent gridAtmosphere, Vector2i tile)
        {
            var tileAtmosphere = GetTileAtmosphere(gridAtmosphere, tile);

            if (tileAtmosphere != null)
                return tileAtmosphere;

            // That tile must be space, or something has gone horribly wrong!
            DebugTools.Assert(IsTileSpace(mapGrid, tile));
            return new TileAtmosphere(mapGrid.Index, tile, new GasMixture(Atmospherics.CellVolume) {Temperature = Atmospherics.TCMB}, true);
        }

        #endregion

        #region Tile Active Add

        /// <summary>
        ///     Makes a tile become active and start processing.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        public void AddActiveTile(EntityCoordinates coordinates)
        {
            if(TryGetGridAndTile(coordinates, out var tuple))
                AddActiveTile(tuple.Value.Grid, tuple.Value.Tile);
        }

        /// <summary>
        ///     Makes a tile become active and start processing.
        /// </summary>
        /// <param name="grid">Grid where to get the tile.</param>
        /// <param name="tile">Indices of the tile to be activated.</param>
        public void AddActiveTile(GridId grid, Vector2i tile)
        {
            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return;

            if (EntityManager.TryGetComponent(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                AddActiveTile(gridAtmosphere, tile);
                return;
            }
        }

        /// <summary>
        ///     Makes a tile become active and start processing.
        /// </summary>
        /// <param name="gridAtmosphere">Grid Atmosphere where to get the tile.</param>
        /// <param name="tile">Indices of the tile to be activated.</param>
        public void AddActiveTile(GridAtmosphereComponent gridAtmosphere, Vector2i tile)
        {
            if (!gridAtmosphere.Tiles.TryGetValue(tile, out var tileAtmosphere))
                return;

            AddActiveTile(gridAtmosphere, tileAtmosphere);
        }

        /// <summary>
        ///     Makes a tile become active and start processing. Does NOT check if the tile belongs to the grid atmos.
        /// </summary>
        /// <param name="gridAtmosphere">Grid Atmosphere where to get the tile.</param>
        /// <param name="tile">Tile Atmosphere to be activated.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddActiveTile(GridAtmosphereComponent gridAtmosphere, TileAtmosphere tile)
        {
            if (tile.Air == null)
                return;

            tile.Excited = true;
            gridAtmosphere.ActiveTiles.Add(tile);
        }

        #endregion

        #region Tile Active Remove

        /// <summary>
        ///     Makes a tile become inactive and stop processing.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        /// <param name="disposeExcitedGroup">Whether to dispose of the tile's <see cref="ExcitedGroup"/></param>
        public void RemoveActiveTile(EntityCoordinates coordinates, bool disposeExcitedGroup = true)
        {
            if(TryGetGridAndTile(coordinates, out var tuple))
                RemoveActiveTile(tuple.Value.Grid, tuple.Value.Tile, disposeExcitedGroup);
        }

        /// <summary>
        ///     Makes a tile become inactive and stop processing.
        /// </summary>
        /// <param name="grid">Grid where to get the tile.</param>
        /// <param name="tile">Indices of the tile to be deactivated.</param>
        /// <param name="disposeExcitedGroup">Whether to dispose of the tile's <see cref="ExcitedGroup"/></param>
        public void RemoveActiveTile(GridId grid, Vector2i tile, bool disposeExcitedGroup = true)
        {
            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return;

            if (EntityManager.TryGetComponent(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                RemoveActiveTile(gridAtmosphere, tile);
                return;
            }
        }

        /// <summary>
        ///     Makes a tile become inactive and stop processing.
        /// </summary>
        /// <param name="gridAtmosphere">Grid Atmosphere where to get the tile.</param>
        /// <param name="tile">Indices of the tile to be deactivated.</param>
        /// <param name="disposeExcitedGroup">Whether to dispose of the tile's <see cref="ExcitedGroup"/></param>
        public void RemoveActiveTile(GridAtmosphereComponent gridAtmosphere, Vector2i tile, bool disposeExcitedGroup = true)
        {
            if (!gridAtmosphere.Tiles.TryGetValue(tile, out var tileAtmosphere))
                return;

            RemoveActiveTile(gridAtmosphere, tileAtmosphere, disposeExcitedGroup);
        }

        /// <summary>
        ///     Makes a tile become inactive and stop processing.
        /// </summary>
        /// <param name="gridAtmosphere">Grid Atmosphere where to get the tile.</param>
        /// <param name="tile">Tile Atmosphere to be deactivated.</param>
        /// <param name="disposeExcitedGroup">Whether to dispose of the tile's <see cref="ExcitedGroup"/></param>
        private void RemoveActiveTile(GridAtmosphereComponent gridAtmosphere, TileAtmosphere tile, bool disposeExcitedGroup = true)
        {
            tile.Excited = false;
            gridAtmosphere.ActiveTiles.Remove(tile);

            if (tile.ExcitedGroup == null)
                return;

            if (disposeExcitedGroup)
                ExcitedGroupDispose(gridAtmosphere, tile.ExcitedGroup);
            else
                ExcitedGroupRemoveTile(tile.ExcitedGroup, tile);
        }

        #endregion

        #region Tile Mixture

        /// <summary>
        ///     Returns a reference to the gas mixture on a tile, or null.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        /// <param name="invalidate">Whether to invalidate the tile.</param>
        /// <returns>The tile mixture, or null</returns>
        public GasMixture? GetTileMixture(EntityCoordinates coordinates, bool invalidate = false)
        {
            return TryGetGridAndTile(coordinates, out var tuple)
                ? GetTileMixture(tuple.Value.Grid, tuple.Value.Tile, invalidate) : GasMixture.SpaceGas;
        }

        /// <summary>
        ///     Returns a reference to the gas mixture on a tile, or null.
        /// </summary>
        /// <param name="grid">Grid where to get the tile air.</param>
        /// <param name="tile">Indices of the tile.</param>
        /// <param name="invalidate">Whether to invalidate the tile.</param>
        /// <returns>The tile mixture, or null</returns>
        public GasMixture? GetTileMixture(GridId grid, Vector2i tile, bool invalidate = false)
        {
            // Always return space gas mixtures for invalid grids (grid 0)
            if (!grid.IsValid())
                return GasMixture.SpaceGas;

            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return null;

            if (EntityManager.TryGetComponent(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                return GetTileMixture(gridAtmosphere, tile, invalidate);
            }

            if (EntityManager.TryGetComponent(mapGrid.GridEntityId, out SpaceAtmosphereComponent? _))
            {
                // Always return a new space gas mixture in this case.
                return GasMixture.SpaceGas;
            }

            return null;
        }

        /// <summary>
        ///     Returns a reference to the gas mixture on a tile, or null.
        /// </summary>
        /// <param name="gridAtmosphere">Grid Atmosphere where to get the tile air.</param>
        /// <param name="tile">Indices of the tile.</param>
        /// <param name="invalidate">Whether to invalidate the tile.</param>
        /// <returns>The tile mixture, or null</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GasMixture? GetTileMixture(GridAtmosphereComponent gridAtmosphere, Vector2i tile, bool invalidate = false)
        {
            if (!gridAtmosphere.Tiles.TryGetValue(tile, out var tileAtmosphere))
                return null;

            // Invalidate the tile if needed.
            if (invalidate)
                InvalidateTile(gridAtmosphere, tile);

            // Return actual tile air or null.
            return tileAtmosphere.Air;
        }

        #endregion

        #region Tile React

        /// <summary>
        ///     Causes a gas mixture reaction on a specific tile.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        /// <returns>Reaction results.</returns>
        public ReactionResult React(EntityCoordinates coordinates)
        {
            if (TryGetGridAndTile(coordinates, out var tuple))
                return React(tuple.Value.Grid, tuple.Value.Tile);

            return ReactionResult.NoReaction;
        }

        /// <summary>
        ///     Causes a gas mixture reaction on a specific tile.
        /// </summary>
        /// <param name="grid">Grid where to get the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        /// <returns>Reaction results.</returns>
        public ReactionResult React(GridId grid, Vector2i tile)
        {
            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return ReactionResult.NoReaction;

            if (EntityManager.TryGetComponent(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                return React(gridAtmosphere, tile);
            }

            return ReactionResult.NoReaction;
        }

        /// <summary>
        ///     Causes a gas mixture reaction on a specific tile.
        /// </summary>
        /// <param name="gridAtmosphere">Grid Atmosphere where to get the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        /// <returns>Reaction results.</returns>
        public ReactionResult React(GridAtmosphereComponent gridAtmosphere, Vector2i tile)
        {
            if (!gridAtmosphere.Tiles.TryGetValue(tile, out var tileAtmosphere) || tileAtmosphere.Air == null)
                return ReactionResult.NoReaction;

            InvalidateTile(gridAtmosphere, tile);

            return React(tileAtmosphere.Air, tileAtmosphere);
        }

        #endregion

        #region Tile Air-blocked

        /// <summary>
        ///     Returns if the tile in question is "air-blocked" in a certain direction or not.
        ///     This could be due to a number of reasons, such as walls, doors, etc.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        /// <param name="direction">Directions to check.</param>
        /// <returns>Whether the tile is blocked in the directions specified.</returns>
        public bool IsTileAirBlocked(EntityCoordinates coordinates, AtmosDirection direction = AtmosDirection.All)
        {
            if (TryGetGridAndTile(coordinates, out var tuple))
                return IsTileAirBlocked(tuple.Value.Grid, tuple.Value.Tile, direction);

            return false;
        }

        /// <summary>
        ///     Returns if the tile in question is "air-blocked" in a certain direction or not.
        ///     This could be due to a number of reasons, such as walls, doors, etc.
        /// </summary>
        /// <param name="grid">Grid where to get the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        /// <param name="direction">Directions to check.</param>
        /// <returns>Whether the tile is blocked in the directions specified.</returns>
        public bool IsTileAirBlocked(GridId grid, Vector2i tile, AtmosDirection direction = AtmosDirection.All)
        {
            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return false;

            return IsTileAirBlocked(mapGrid, tile, direction);
        }

        /// <summary>
        ///     Returns if the tile in question is "air-blocked" in a certain direction or not.
        ///     This could be due to a number of reasons, such as walls, doors, etc.
        /// </summary>
        /// <param name="mapGrid">Grid where to get the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        /// <param name="direction">Directions to check.</param>
        /// <returns>Whether the tile is blocked in the directions specified.</returns>
        public bool IsTileAirBlocked(IMapGrid mapGrid, Vector2i tile, AtmosDirection direction = AtmosDirection.All)
        {
            var directions = AtmosDirection.Invalid;

            foreach (var obstructingComponent in GetObstructingComponents(mapGrid, tile))
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

        #endregion

        #region Tile Space

        /// <summary>
        ///     Returns whether the specified tile is a space tile or not.
        /// </summary>
        /// <param name="coordinates">Coordinates where to check the tile.</param>
        /// <returns>Whether the tile is space or not.</returns>
        public bool IsTileSpace(EntityCoordinates coordinates)
        {
            if (TryGetGridAndTile(coordinates, out var tuple))
                return IsTileSpace(tuple.Value.Grid, tuple.Value.Tile);

            return true;
        }

        /// <summary>
        ///     Returns whether the specified tile is a space tile or not.
        /// </summary>
        /// <param name="grid">Grid where to check the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        /// <returns>Whether the tile is space or not.</returns>
        public bool IsTileSpace(GridId grid, Vector2i tile)
        {
            return !_mapManager.TryGetGrid(grid, out var mapGrid) || IsTileSpace(mapGrid, tile);
        }

        public bool IsTileSpace(IMapGrid mapGrid, Vector2i tile)
        {
            if (!mapGrid.TryGetTileRef(tile, out var tileRef))
                return true;

            return ((ContentTileDefinition) _tileDefinitionManager[tileRef.Tile.TypeId]).IsSpace;
        }

        #endregion

        #region Adjacent Get Positions

        /// <summary>
        ///     Gets all the positions adjacent to a tile. Can include air-blocked directions.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        /// <param name="includeBlocked">Whether to include tiles in directions the tile is air-blocked in.</param>
        /// <returns>The positions adjacent to the tile.</returns>
        public IEnumerable<Vector2i> GetAdjacentTiles(EntityCoordinates coordinates, bool includeBlocked = false)
        {
            if (TryGetGridAndTile(coordinates, out var tuple))
                return GetAdjacentTiles(tuple.Value.Grid, tuple.Value.Tile, includeBlocked);

            return Enumerable.Empty<Vector2i>();
        }

        /// <summary>
        ///     Gets all the positions adjacent to a tile. Can include air-blocked directions.
        /// </summary>
        /// <param name="grid">Grid where to get the tiles.</param>
        /// <param name="tile">Indices of the tile.</param>
        /// <param name="includeBlocked">Whether to include tiles in directions the tile is air-blocked in.</param>
        /// <returns>The positions adjacent to the tile.</returns>
        public IEnumerable<Vector2i> GetAdjacentTiles(GridId grid, Vector2i tile, bool includeBlocked = false)
        {
            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return Enumerable.Empty<Vector2i>();

            if (EntityManager.TryGetComponent(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                return GetAdjacentTiles(gridAtmosphere, tile, includeBlocked);
            }

            return Enumerable.Empty<Vector2i>();
        }

        /// <summary>
        ///     Gets all the positions adjacent to a tile. Can include air-blocked directions.
        /// </summary>
        /// <param name="gridAtmosphere">Grid Atmosphere where to get the tiles.</param>
        /// <param name="tile">Indices of the tile.</param>
        /// <param name="includeBlocked">Whether to include tiles in directions the tile is air-blocked in.</param>
        /// <returns>The positions adjacent to the tile.</returns>
        public IEnumerable<Vector2i> GetAdjacentTiles(GridAtmosphereComponent gridAtmosphere, Vector2i tile, bool includeBlocked = false)
        {
            if(!gridAtmosphere.Tiles.TryGetValue(tile, out var tileAtmosphere))
                yield break;

            for (var i = 0; i < tileAtmosphere.AdjacentTiles.Length; i++)
            {
                var adjacentTile = tileAtmosphere.AdjacentTiles[i];
                // TileAtmosphere has nullable disabled, so just in case...
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (adjacentTile?.Air == null)
                    continue;

                if (!includeBlocked)
                {
                    var direction = (AtmosDirection) (1 << i);
                    if (tileAtmosphere.BlockedAirflow.IsFlagSet(direction))
                        continue;
                }

                yield return adjacentTile.GridIndices;
            }
        }

        #endregion

        #region Adjacent Get Mixture

        /// <summary>
        ///     Gets all tile gas mixtures adjacent to a specific tile, and optionally invalidates them.
        ///     Does not return the tile in question, only the adjacent ones.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        /// <param name="includeBlocked">Whether to include tiles in directions the tile is air-blocked in.</param>
        /// <param name="invalidate">Whether to invalidate all adjacent tiles.</param>
        /// <returns>All adjacent tile gas mixtures to the tile in question</returns>
        public IEnumerable<GasMixture> GetAdjacentTileMixtures(EntityCoordinates coordinates, bool includeBlocked = false, bool invalidate = false)
        {
            if (TryGetGridAndTile(coordinates, out var tuple))
                return GetAdjacentTileMixtures(tuple.Value.Grid, tuple.Value.Tile);

            return Enumerable.Empty<GasMixture>();
        }

        /// <summary>
        ///     Gets all tile gas mixtures adjacent to a specific tile, and optionally invalidates them.
        ///     Does not return the tile in question, only the adjacent ones.
        /// </summary>
        /// <param name="grid">Grid where to get the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        /// <param name="includeBlocked">Whether to include tiles in directions the tile is air-blocked in.</param>
        /// <param name="invalidate">Whether to invalidate all adjacent tiles.</param>
        /// <returns>All adjacent tile gas mixtures to the tile in question</returns>
        public IEnumerable<GasMixture> GetAdjacentTileMixtures(GridId grid, Vector2i tile, bool includeBlocked = false, bool invalidate = false)
        {
            // For invalid grids, return an array with a single space gas mixture in it.
            if (!grid.IsValid())
                return new []{ GasMixture.SpaceGas };

            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return Enumerable.Empty<GasMixture>();

            if (EntityManager.TryGetComponent(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                return GetAdjacentTileMixtures(gridAtmosphere, tile, includeBlocked, invalidate);
            }

            return Enumerable.Empty<GasMixture>();
        }

        /// <summary>
        ///     Gets all tile gas mixtures adjacent to a specific tile, and optionally invalidates them.
        ///     Does not return the tile in question, only the adjacent ones.
        /// </summary>
        /// <param name="gridAtmosphere">Grid Atmosphere where to get the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        /// <param name="includeBlocked">Whether to include tiles in directions the tile is air-blocked in.</param>
        /// <param name="invalidate">Whether to invalidate all adjacent tiles.</param>
        /// <returns>All adjacent tile gas mixtures to the tile in question</returns>
        public IEnumerable<GasMixture> GetAdjacentTileMixtures(GridAtmosphereComponent gridAtmosphere, Vector2i tile, bool includeBlocked = false, bool invalidate = false)
        {
            if (!gridAtmosphere.Tiles.TryGetValue(tile, out var tileAtmosphere))
                return Enumerable.Empty<GasMixture>();

            return GetAdjacentTileMixtures(gridAtmosphere, tileAtmosphere, includeBlocked, invalidate);
        }

        /// <summary>
        ///     Gets all tile gas mixtures adjacent to a specific tile, and optionally invalidates them.
        ///     Does not return the tile in question, only the adjacent ones.
        /// </summary>
        /// <param name="gridAtmosphere">Grid Atmosphere where the tile is.</param>
        /// <param name="tile">Tile Atmosphere in question.</param>
        /// <param name="includeBlocked">Whether to include tiles in directions the tile is air-blocked in.</param>
        /// <param name="invalidate">Whether to invalidate all adjacent tiles.</param>
        /// <returns>All adjacent tile gas mixtures to the tile in question</returns>
        private IEnumerable<GasMixture> GetAdjacentTileMixtures(GridAtmosphereComponent gridAtmosphere, TileAtmosphere tile, bool includeBlocked = false, bool invalidate = false)
        {
            for (var i = 0; i < tile.AdjacentTiles.Length; i++)
            {
                var adjacentTile = tile.AdjacentTiles[i];

                // TileAtmosphere has nullable disabled, so just in case...
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (adjacentTile?.Air == null)
                    continue;

                if (!includeBlocked)
                {
                    var direction = (AtmosDirection) (1 << i);
                    if (tile.BlockedAirflow.IsFlagSet(direction))
                        continue;
                }

                if (invalidate)
                    InvalidateTile(gridAtmosphere, adjacentTile.GridIndices);

                yield return adjacentTile.Air;
            }
        }

        #endregion

        #region Adjacent Update

        /// <summary>
        ///     Immediately updates a tile's blocked air directions.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        public void UpdateAdjacent(EntityCoordinates coordinates)
        {
            if(TryGetGridAndTile(coordinates, out var tuple))
                UpdateAdjacent(tuple.Value.Grid, tuple.Value.Tile);
        }

        /// <summary>
        ///     Immediately updates a tile's blocked air directions.
        /// </summary>
        /// <param name="grid">Grid where to get the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        public void UpdateAdjacent(GridId grid, Vector2i tile)
        {
            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return;

            if (EntityManager.TryGetComponent(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                UpdateAdjacent(mapGrid, gridAtmosphere, tile);
                return;
            }
        }

        /// <summary>
        ///     Immediately updates a tile's blocked air directions.
        /// </summary>
        /// <param name="mapGrid">Grid where to get the tile.</param>
        /// <param name="gridAtmosphere">Grid Atmosphere where to get the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        public void UpdateAdjacent(IMapGrid mapGrid, GridAtmosphereComponent gridAtmosphere, Vector2i tile)
        {
            if (!gridAtmosphere.Tiles.TryGetValue(tile, out var tileAtmosphere))
                return;

            UpdateAdjacent(mapGrid, gridAtmosphere, tileAtmosphere);
        }

        /// <summary>
        ///     Immediately updates a tile's blocked air directions.
        /// </summary>
        /// <param name="mapGrid">Grid where to get the tile.</param>
        /// <param name="gridAtmosphere">Grid Atmosphere of the tile.</param>
        /// <param name="tileAtmosphere">Tile Atmosphere to be updated.</param>
        private void UpdateAdjacent(IMapGrid mapGrid, GridAtmosphereComponent gridAtmosphere, TileAtmosphere tileAtmosphere)
        {
            tileAtmosphere.AdjacentBits = AtmosDirection.Invalid;

            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var direction = (AtmosDirection) (1 << i);

                var otherIndices = tileAtmosphere.GridIndices.Offset(direction);

                var adjacent = GetTileAtmosphereOrCreateSpace(mapGrid, gridAtmosphere, otherIndices);
                tileAtmosphere.AdjacentTiles[direction.ToIndex()] = adjacent;

                UpdateAdjacent(mapGrid, gridAtmosphere, adjacent, direction.GetOpposite());

                if (!tileAtmosphere.BlockedAirflow.IsFlagSet(direction)
                    && !IsTileAirBlocked(mapGrid, adjacent.GridIndices, direction.GetOpposite()))
                {
                    tileAtmosphere.AdjacentBits |= direction;
                }
            }
        }

        /// <summary>
        ///     Immediately updates a tile's single blocked air direction.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        /// <param name="direction">Direction to be updated.</param>
        public void UpdateAdjacent(EntityCoordinates coordinates, AtmosDirection direction)
        {
            if(TryGetGridAndTile(coordinates, out var tuple))
                UpdateAdjacent(tuple.Value.Grid, tuple.Value.Tile, direction);
        }

        /// <summary>
        ///     Immediately updates a tile's single blocked air direction.
        /// </summary>
        /// <param name="grid">Grid where to get the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        /// <param name="direction">Direction to be updated.</param>
        public void UpdateAdjacent(GridId grid, Vector2i tile, AtmosDirection direction)
        {
            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return;

            if (EntityManager.TryGetComponent(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                UpdateAdjacent(mapGrid, gridAtmosphere, tile, direction);
                return;
            }
        }

        /// <summary>
        ///     Immediately updates a tile's single blocked air direction.
        /// </summary>
        /// <param name="mapGrid">Grid where to get the tile.</param>
        /// <param name="gridAtmosphere">Grid Atmosphere where to get the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        /// <param name="direction">Direction to be updated.</param>
        public void UpdateAdjacent(IMapGrid mapGrid, GridAtmosphereComponent gridAtmosphere, Vector2i tile, AtmosDirection direction)
        {
            if (!gridAtmosphere.Tiles.TryGetValue(tile, out var tileAtmosphere))
                return;

            UpdateAdjacent(mapGrid, gridAtmosphere, tileAtmosphere, direction);
        }

        /// <summary>
        ///     Immediately updates a tile's single blocked air direction.
        /// </summary>
        /// <param name="mapGrid">Grid where to get the tile.</param>
        /// <param name="gridAtmosphere">Grid where to get the tile.</param>
        /// <param name="tile">Tile Atmosphere to be updated.</param>
        /// <param name="direction">Direction to be updated.</param>
        private void UpdateAdjacent(IMapGrid mapGrid, GridAtmosphereComponent gridAtmosphere, TileAtmosphere tile, AtmosDirection direction)
        {
            tile.AdjacentTiles[direction.ToIndex()] = GetTileAtmosphereOrCreateSpace(mapGrid, gridAtmosphere, tile.GridIndices.Offset(direction));

            if (!tile.BlockedAirflow.IsFlagSet(direction) && !IsTileAirBlocked(mapGrid, tile.GridIndices.Offset(direction), direction.GetOpposite()))
            {
                tile.AdjacentBits |= direction;
            }
            else
            {
                tile.AdjacentBits &= ~direction;
            }
        }

        #endregion

        #region Hotspot Expose

        /// <summary>
        ///     Exposes temperature to a tile, creating a hotspot (fire) if the conditions are ideal.
        ///     Can also be used to make an existing hotspot hotter/bigger. Also invalidates the tile.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        /// <param name="exposedTemperature">Temperature to expose to the tile.</param>
        /// <param name="exposedVolume">Volume of the exposed temperature.</param>
        /// <param name="soh">If true, the existing hotspot values will be set to the exposed values, but only if they're smaller.</param>
        public void HotspotExpose(EntityCoordinates coordinates, float exposedTemperature, float exposedVolume, bool soh = false)
        {
            if(TryGetGridAndTile(coordinates, out var tuple))
                HotspotExpose(tuple.Value.Grid, tuple.Value.Tile, exposedTemperature, exposedVolume, soh);
        }

        /// <summary>
        ///     Exposes temperature to a tile, creating a hotspot (fire) if the conditions are ideal.
        ///     Can also be used to make an existing hotspot hotter/bigger. Also invalidates the tile.
        /// </summary>
        /// <param name="grid">Grid where to get the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        /// <param name="exposedTemperature">Temperature to expose to the tile.</param>
        /// <param name="exposedVolume">Volume of the exposed temperature.</param>
        /// <param name="soh">If true, the existing hotspot values will be set to the exposed values, but only if they're smaller.</param>
        public void HotspotExpose(GridId grid, Vector2i tile, float exposedTemperature, float exposedVolume, bool soh = false)
        {
            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return;

            if (EntityManager.TryGetComponent(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                var tileAtmosphere = GetTileAtmosphere(gridAtmosphere, tile);

                if (tileAtmosphere == null)
                    return;

                HotspotExpose(gridAtmosphere, tileAtmosphere, exposedTemperature, exposedVolume, soh);
                InvalidateTile(gridAtmosphere, tile);
                return;
            }
        }

        #endregion

        #region Hotspot Extinguish

        /// <summary>
        ///     Extinguishes a hotspot (fire) on a certain tile, if any. Also invalidates the tile.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        public void HotspotExtinguish(EntityCoordinates coordinates)
        {
            if(TryGetGridAndTile(coordinates, out var tuple))
                HotspotExtinguish(tuple.Value.Grid, tuple.Value.Tile);
        }

        /// <summary>
        ///     Extinguishes a hotspot (fire) on a certain tile, if any. Also invalidates the tile.
        /// </summary>
        /// <param name="grid">Grid where to get the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        public void HotspotExtinguish(GridId grid, Vector2i tile)
        {
            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return;

            if (EntityManager.TryGetComponent(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                HotspotExtinguish(gridAtmosphere, tile);
                return;
            }
        }

        /// <summary>
        ///     Extinguishes a hotspot (fire) on a certain tile, if any. Also invalidates the tile.
        /// </summary>
        /// <param name="gridAtmosphere">Grid Atmosphere where to get the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        public void HotspotExtinguish(GridAtmosphereComponent gridAtmosphere, Vector2i tile)
        {
            if (!gridAtmosphere.Tiles.TryGetValue(tile, out var tileAtmosphere))
                return;

            tileAtmosphere.Hotspot = new Hotspot();
            InvalidateTile(gridAtmosphere, tile);
        }

        #endregion

        #region Hotspot Active

        /// <summary>
        ///     Returns whether there's an active hotspot (fire) on a certain tile.
        /// </summary>
        /// <param name="coordinates">Position where to get the tile.</param>
        /// <returns>Whether the hotspot is active or not.</returns>
        public bool IsHotspotActive(EntityCoordinates coordinates)
        {
            if (TryGetGridAndTile(coordinates, out var tuple))
                return IsHotspotActive(tuple.Value.Grid, tuple.Value.Tile);

            return false;
        }

        /// <summary>
        ///     Returns whether there's an active hotspot (fire) on a certain tile.
        /// </summary>
        /// <param name="grid">Grid where to get the tile</param>
        /// <param name="tile">Indices for the tile</param>
        /// <returns>Whether the hotspot is active or not.</returns>
        public bool IsHotspotActive(GridId grid, Vector2i tile)
        {
            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return false;

            if (EntityManager.TryGetComponent(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                return IsHotspotActive(gridAtmosphere, tile);
            }

            return false;
        }

        /// <summary>
        ///     Returns whether there's an active hotspot (fire) on a certain tile.
        /// </summary>
        /// <param name="gridAtmosphere">Grid Atmosphere where to get the tile</param>
        /// <param name="tile">Indices for the tile</param>
        /// <returns>Whether the hotspot is active or not.</returns>
        public bool IsHotspotActive(GridAtmosphereComponent gridAtmosphere, Vector2i tile)
        {
            if (!gridAtmosphere.Tiles.TryGetValue(tile, out var tileAtmosphere))
                return false;

            return tileAtmosphere.Hotspot.Valid;
        }

        #endregion

        #region PipeNet Add

        public void AddPipeNet(PipeNet pipeNet)
        {
            if (!_mapManager.TryGetGrid(pipeNet.Grid, out var mapGrid))
                return;

            if (EntityManager.TryGetComponent(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                gridAtmosphere.PipeNets.Add(pipeNet);
            }
        }

        #endregion

        #region PipeNet Remove

        public void RemovePipeNet(PipeNet pipeNet)
        {
            if (!_mapManager.TryGetGrid(pipeNet.Grid, out var mapGrid))
                return;

            if (EntityManager.TryGetComponent(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                gridAtmosphere.PipeNets.Remove(pipeNet);
            }
        }

        #endregion

        #region AtmosDevice Add

        public bool AddAtmosDevice(AtmosDeviceComponent atmosDevice)
        {
            var grid = atmosDevice.Owner.Transform.GridID;

            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return false;

            if (EntityManager.TryGetComponent(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                atmosDevice.JoinedGrid = grid;
                gridAtmosphere.AtmosDevices.Add(atmosDevice);
                return true;
            }

            return false;
        }

        #endregion

        #region AtmosDevice Remove

        public bool RemoveAtmosDevice(AtmosDeviceComponent atmosDevice)
        {
            if (atmosDevice.JoinedGrid == null)
                return false;

            var grid = atmosDevice.JoinedGrid.Value;

            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return false;

            if (EntityManager.TryGetComponent(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere)
                && gridAtmosphere.AtmosDevices.Contains(atmosDevice))
            {
                atmosDevice.JoinedGrid = null;
                gridAtmosphere.AtmosDevices.Remove(atmosDevice);
                return true;
            }

            return false;
        }

        #endregion

        #region Mixture Safety

        /// <summary>
        ///     Checks whether a tile's gas mixture is probably safe.
        ///     This only checks temperature and pressure, not gas composition.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        /// <returns>Whether the tile's gas mixture is probably safe.</returns>
        public bool IsTileMixtureProbablySafe(EntityCoordinates coordinates)
        {
            return IsMixtureProbablySafe(GetTileMixture(coordinates));
        }

        /// <summary>
        ///     Checks whether a tile's gas mixture is probably safe.
        ///     This only checks temperature and pressure, not gas composition.
        /// </summary>
        /// <param name="grid">Grid where to get the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        /// <returns>Whether the tile's gas mixture is probably safe.</returns>
        public bool IsTileMixtureProbablySafe(GridId grid, Vector2i tile)
        {
            return IsMixtureProbablySafe(GetTileMixture(grid, tile));
        }

        /// <summary>
        ///     Checks whether a gas mixture is probably safe.
        ///     This only checks temperature and pressure, not gas composition.
        /// </summary>
        /// <param name="air">Mixture to be checked.</param>
        /// <returns>Whether the mixture is probably safe.</returns>
        public bool IsMixtureProbablySafe(GasMixture? air)
        {
            // Note that oxygen mix isn't checked, but survival boxes make that not necessary.
            if (air == null)
                return false;

            switch (air.Pressure)
            {
                case <= Atmospherics.WarningLowPressure:
                case >= Atmospherics.WarningHighPressure:
                    return false;
            }

            switch (air.Temperature)
            {
                case <= 260:
                case >= 360:
                    return false;
            }

            return true;
        }

        #endregion

        #region Fix Vacuum

        /// <summary>
        ///     Attempts to fix a sudden vacuum by creating gas based on adjacent tiles.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        public void FixVacuum(EntityCoordinates coordinates)
        {
            if(TryGetGridAndTile(coordinates, out var tuple))
                FixVacuum(tuple.Value.Grid, tuple.Value.Tile);
        }

        /// <summary>
        ///     Attempts to fix a sudden vacuum by creating gas based on adjacent tiles.
        /// </summary>
        /// <param name="grid">Grid where to get the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        public void FixVacuum(GridId grid, Vector2i tile)
        {
            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return;

            if (EntityManager.TryGetComponent(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                FixVacuum(gridAtmosphere, tile);
                return;
            }
        }

        public void FixVacuum(GridAtmosphereComponent gridAtmosphere, Vector2i tile)
        {
            if (!gridAtmosphere.Tiles.TryGetValue(tile, out var tileAtmosphere))
                return;

            var adjacent = GetAdjacentTileMixtures(gridAtmosphere, tileAtmosphere, false, true).ToArray();
            tileAtmosphere.Air = new GasMixture(GetVolumeForTiles(tileAtmosphere.GridIndex, 1))
                {Temperature = Atmospherics.T20C};

            // Return early, let's not cause any funny NaNs.
            if (adjacent.Length == 0)
                return;

            var ratio = 1f / adjacent.Length;
            var totalTemperature = 0f;

            foreach (var adj in adjacent)
            {
                totalTemperature += adj.Temperature;

                // Remove a bit of gas from the adjacent ratio...
                var mix = adj.RemoveRatio(ratio);

                // And merge it to the new tile air.
                Merge(tileAtmosphere.Air, mix);

                // Return removed gas to its original mixture.
                Merge(adj, mix);
            }

            // New temperature is the arithmetic mean of the sum of the adjacent temperatures...
            tileAtmosphere.Air.Temperature = totalTemperature / adjacent.Length;
        }

        public bool NeedsVacuumFixing(IMapGrid mapGrid, Vector2i indices)
        {
            var value = false;

            foreach (var airtightComponent in GetObstructingComponents(mapGrid, indices))
            {
                value |= airtightComponent.FixVacuum;
            }

            return value;
        }

        #endregion

        #region Position Helpers

        public bool TryGetGridAndTile(MapCoordinates coordinates, [NotNullWhen(true)] out (GridId Grid, Vector2i Tile)? tuple)
        {
            if (!_mapManager.TryFindGridAt(coordinates, out var grid))
            {
                tuple = null;
                return false;
            }

            tuple = (grid.Index, grid.TileIndicesFor(coordinates));
            return true;
        }

        public bool TryGetGridAndTile(EntityCoordinates coordinates, [NotNullWhen(true)] out (GridId Grid, Vector2i Tile)? tuple)
        {
            if (!coordinates.IsValid(EntityManager))
            {
                tuple = null;
                return false;
            }

            var gridId = coordinates.GetGridId(EntityManager);

            if (!_mapManager.TryGetGrid(gridId, out var grid))
            {
                tuple = null;
                return false;
            }

            tuple = (gridId, grid.TileIndicesFor(coordinates));
            return true;
        }

        public bool TryGetMapGrid(GridAtmosphereComponent gridAtmosphere, [NotNullWhen(true)] out IMapGrid? mapGrid)
        {
            if (gridAtmosphere.Owner.TryGetComponent(out IMapGridComponent? mapGridComponent))
            {
                mapGrid = mapGridComponent.Grid;
                return true;
            }

            mapGrid = null;
            return false;
        }

        #endregion
    }
}
