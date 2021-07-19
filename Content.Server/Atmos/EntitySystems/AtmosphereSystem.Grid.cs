using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.Reactions;
using Content.Shared.Atmos;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.Atmos.EntitySystems
{
    public partial class AtmosphereSystem
    {
        #region Get GridAtmosphere

        public IGridAtmosphereComponent? GetGridAtmosphere(GridId gridId)
        {
            if (!gridId.IsValid())
                return null;

            if (!_mapManager.TryGetGrid(gridId, out var grid))
                return null;

            return ComponentManager.TryGetComponent(grid.GridEntityId, out IGridAtmosphereComponent? gridAtmosphere)
                ? gridAtmosphere : null;
        }

        public IGridAtmosphereComponent GetGridAtmosphere(EntityCoordinates coordinates)
        {
            return GetGridAtmosphere(coordinates.ToMap(EntityManager));
        }

        public IGridAtmosphereComponent GetGridAtmosphere(MapCoordinates coordinates)
        {
            if (coordinates.MapId == MapId.Nullspace)
            {
                throw new ArgumentException($"Coordinates cannot be in nullspace!", nameof(coordinates));
            }

            if (_mapManager.TryFindGridAt(coordinates, out var grid))
            {
                if (ComponentManager.TryGetComponent(grid.GridEntityId, out IGridAtmosphereComponent? atmos))
                {
                    return atmos;
                }
            }

            return _mapManager.GetMapEntity(coordinates.MapId).GetComponent<IGridAtmosphereComponent>();
        }

        /// <summary>
        ///     Unlike GetGridAtmosphere, this doesn't return space grid when not found.
        /// </summary>
        public bool TryGetSimulatedGridAtmosphere(MapCoordinates coordinates, [NotNullWhen(true)] out IGridAtmosphereComponent? atmosphere)
        {
            if (coordinates.MapId == MapId.Nullspace)
            {
                atmosphere = null;
                return false;
            }

            if (_mapManager.TryFindGridAt(coordinates, out var mapGrid)
                && ComponentManager.TryGetComponent(mapGrid.GridEntityId, out IGridAtmosphereComponent? atmosGrid)
                && atmosGrid.Simulated)
            {
                atmosphere = atmosGrid;
                return true;
            }

            atmosphere = null;
            return false;
        }

        #endregion

        #region Grid Is Simulated

        /// <summary>
        ///     Returns whether a grid has a simulated atmosphere.
        /// </summary>
        /// <param name="coordinates">Coordinates to be checked.</param>
        /// <returns>Whether the grid has a simulated atmosphere.</returns>
        public bool IsSimulatedGrid(MapCoordinates coordinates)
        {
            if (TryGetGridAndTile(coordinates, out var tuple))
                return IsSimulatedGrid(tuple.Value.Grid);

            return false;
        }

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

            if (ComponentManager.HasComponent<GridAtmosphereComponent>(mapGrid.GridEntityId))
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
        public IEnumerable<GasMixture> GetAllTileMixtures(MapCoordinates coordinates, bool invalidate = false)
        {
            if (TryGetGridAndTile(coordinates, out var tuple))
                return GetAllTileMixtures(tuple.Value.Grid, invalidate);

            return Enumerable.Empty<GasMixture>();
        }

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
            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                yield break;

            if (ComponentManager.TryGetComponent(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                foreach (var tileAtmos in gridAtmosphere)
                {
                    if (tileAtmos?.Air == null)
                        continue;

                    if(invalidate)
                        tileAtmos.Invalidate();

                    yield return tileAtmos.Air;
                }
            }
        }

        #endregion

        #region Invalidate

        /// <summary>
        ///     Invalidates a tile at a certain position.
        /// </summary>
        /// <param name="coordinates"></param>
        public void InvalidateTile(MapCoordinates coordinates)
        {
            if(TryGetGridAndTile(coordinates, out var tuple))
                InvalidateTile(tuple.Value.Grid, tuple.Value.Tile);
        }

        /// <summary>
        ///     Invalidates a tile at a certain position.
        /// </summary>
        /// <param name="coordinates"></param>
        public void InvalidateTile(EntityCoordinates coordinates)
        {
            if(TryGetGridAndTile(coordinates, out var tuple))
                InvalidateTile(tuple.Value.Grid, tuple.Value.Tile);
        }

        /// <summary>
        ///     Invalidates a tile at a certain position.
        /// </summary>
        /// <param name="grid">Grid where to invalidate the tile.</param>
        /// <param name="position">The tile's indices.</param>
        public void InvalidateTile(GridId grid, Vector2i position)
        {
            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return;

            if (ComponentManager.TryGetComponent(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                gridAtmosphere.Invalidate(position);
                return;
            }
        }

        #endregion

        #region Tile Active Add

        /// <summary>
        ///     Makes a tile become active and start processing.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        public void AddActiveTile(MapCoordinates coordinates)
        {
            if(TryGetGridAndTile(coordinates, out var tuple))
                AddActiveTile(tuple.Value.Grid, tuple.Value.Tile);
        }

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

            if (ComponentManager.TryGetComponent(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                var tileAtmosphere = gridAtmosphere.GetTile(tile)!;
                gridAtmosphere.AddActiveTile(tileAtmosphere);
                return;
            }
        }

        #endregion

        #region Tile Active Remove

        /// <summary>
        ///     Makes a tile become inactive and stop processing.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        /// <param name="disposeExcitedGroup">Whether to dispose of the tile's <see cref="ExcitedGroup"/></param>
        public void RemoveActiveTile(MapCoordinates coordinates, bool disposeExcitedGroup = false)
        {
            if(TryGetGridAndTile(coordinates, out var tuple))
                RemoveActiveTile(tuple.Value.Grid, tuple.Value.Tile, disposeExcitedGroup);
        }

        /// <summary>
        ///     Makes a tile become inactive and stop processing.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        /// <param name="disposeExcitedGroup">Whether to dispose of the tile's <see cref="ExcitedGroup"/></param>
        public void RemoveActiveTile(EntityCoordinates coordinates, bool disposeExcitedGroup = false)
        {
            if(TryGetGridAndTile(coordinates, out var tuple))
                RemoveActiveTile(tuple.Value.Grid, tuple.Value.Tile, disposeExcitedGroup);
        }

        /// <summary>
        ///     Makes a tile become inactive and stop processing.
        /// </summary>
        /// <param name="grid">Grid where to get the tile.</param>
        /// <param name="tile">Indices of the tile to be activated.</param>
        /// <param name="disposeExcitedGroup">Whether to dispose of the tile's <see cref="ExcitedGroup"/></param>
        public void RemoveActiveTile(GridId grid, Vector2i tile, bool disposeExcitedGroup = false)
        {
            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return;

            if (ComponentManager.TryGetComponent(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                var tileAtmosphere = gridAtmosphere.GetTile(tile)!;
                gridAtmosphere.RemoveActiveTile(tileAtmosphere, disposeExcitedGroup);
                return;
            }
        }

        #endregion

        #region Tile Mixture

        /// <summary>
        ///     Returns a reference to the gas mixture on a tile, or null.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        /// <param name="invalidate">Whether to invalidate the tile.</param>
        /// <returns>The tile mixture, or null</returns>
        public GasMixture? GetTileMixture(MapCoordinates coordinates, bool invalidate = false)
        {
            return TryGetGridAndTile(coordinates, out var tuple)
                ? GetTileMixture(tuple.Value.Grid, tuple.Value.Tile, invalidate) : null;
        }

        /// <summary>
        ///     Returns a reference to the gas mixture on a tile, or null.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        /// <param name="invalidate">Whether to invalidate the tile.</param>
        /// <returns>The tile mixture, or null</returns>
        public GasMixture? GetTileMixture(EntityCoordinates coordinates, bool invalidate = false)
        {
            return TryGetGridAndTile(coordinates, out var tuple)
                ? GetTileMixture(tuple.Value.Grid, tuple.Value.Tile, invalidate) : null;
        }

        /// <summary>
        ///     Returns a reference to the gas mixture on a tile, or null.
        /// </summary>
        /// <param name="grid">Grid where to get the tile air.</param>
        /// <param name="position">Indices of the tile.</param>
        /// <param name="invalidate">Whether to invalidate the tile.</param>
        /// <returns>The tile mixture, or null</returns>
        public GasMixture? GetTileMixture(GridId grid, Vector2i position, bool invalidate = false)
        {
            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return null;

            if (ComponentManager.TryGetComponent(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                // Invalidate the tile if needed.
                if(invalidate)
                    gridAtmosphere.Invalidate(position);

                // Return actual tile air or null.
                return gridAtmosphere.GetTile(position)?.Air;
            }

            if (ComponentManager.TryGetComponent(mapGrid.GridEntityId, out SpaceGridAtmosphereComponent? spaceAtmosphere))
            {
                // Always return a new space gas mixture in this case.
                return GasMixture.SpaceGas;
            }

            return null;
        }

        #endregion

        #region Tile React

        /// <summary>
        ///     Causes a gas mixture reaction on a specific tile.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        /// <returns>Reaction results.</returns>
        public ReactionResult React(MapCoordinates coordinates)
        {
            if (TryGetGridAndTile(coordinates, out var tuple))
                return React(tuple.Value.Grid, tuple.Value.Tile);

            return ReactionResult.NoReaction;
        }

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
            var atmosphere = GetGridAtmosphere(grid);
            atmosphere?.Invalidate(tile);

            var tileAtmosphere = atmosphere?.GetTile(tile);
            return tileAtmosphere?.Air == null ? ReactionResult.NoReaction : React(tileAtmosphere.Air, tileAtmosphere);
        }

        #endregion

        #region Tile Airblocked

        /// <summary>
        ///     Returns if the tile in question is "air-blocked" in a certain direction or not.
        ///     This could be due to a number of reasons, such as walls, doors, etc.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        /// <param name="direction">Directions to check.</param>
        /// <returns>Whether the tile is blocked in the directions specified.</returns>
        public bool IsTileAirBlocked(MapCoordinates coordinates, AtmosDirection direction = AtmosDirection.All)
        {
            if (TryGetGridAndTile(coordinates, out var tuple))
                return IsTileAirBlocked(tuple.Value.Grid, tuple.Value.Tile, direction);

            return false;
        }

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

            if (ComponentManager.TryGetComponent(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                return gridAtmosphere.IsAirBlocked(tile, direction);
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
        public bool IsTileSpace(MapCoordinates coordinates)
        {
            if (TryGetGridAndTile(coordinates, out var tuple))
                return IsTileSpace(tuple.Value.Grid, tuple.Value.Tile);

            return true;
        }

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
            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return true;

            if (ComponentManager.TryGetComponent(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                return gridAtmosphere.IsSpace(tile);
            }

            return true;
        }

        #endregion

        #region Adjacent Get Positions

        /// <summary>
        ///     Gets all the positions adjacent to a tile. Can include air-blocked directions.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        /// <param name="includeBlocked">Whether to include tiles in directions the tile is air-blocked in.</param>
        /// <returns>The positions adjacent to the tile.</returns>
        public IEnumerable<Vector2i> GetAdjacentTiles(MapCoordinates coordinates, bool includeBlocked = false)
        {
            if (TryGetGridAndTile(coordinates, out var tuple))
                return GetAdjacentTiles(tuple.Value.Grid, tuple.Value.Tile, includeBlocked);

            return Enumerable.Empty<Vector2i>();
        }

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
                yield break;

            if (ComponentManager.TryGetComponent(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                var tileAtmos = gridAtmosphere.GetTile(tile)!;

                for (var i = 0; i < tileAtmos.AdjacentTiles.Count; i++)
                {
                    var adjacentTile = tileAtmos.AdjacentTiles[i];
                    // TileAtmosphere has nullable disabled, so just in case...
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                    if (adjacentTile?.Air == null)
                        continue;

                    if (!includeBlocked)
                    {
                        var direction = (AtmosDirection) (1 << i);
                        if (tileAtmos.BlockedAirflow.IsFlagSet(direction))
                            continue;
                    }

                    yield return adjacentTile.GridIndices;
                }
            }
        }

        #endregion

        #region Adjacent Get Mixture

        /// <summary>
        ///     Gets all tile gas mixtures adjacent to a specific tile, and optionally invalidates them.
        ///     Does not return the tile in question, only the adjacent ones. Can include air-blocked directions.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        /// <param name="includeBlocked">Whether to include tiles in directions the tile is air-blocked in.</param>
        /// <param name="invalidate">Whether to invalidate all adjacent tiles.</param>
        /// <returns>All adjacent tile gas mixtures to the tile in question</returns>
        public IEnumerable<GasMixture> GetAdjacentTileMixtures(MapCoordinates coordinates, bool includeBlocked = false, bool invalidate = false)
        {
            if (TryGetGridAndTile(coordinates, out var tuple))
                return GetAdjacentTileMixtures(tuple.Value.Grid, tuple.Value.Tile);

            return Enumerable.Empty<GasMixture>();
        }

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
            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                yield break;

            if (ComponentManager.TryGetComponent(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                var tileAtmos = gridAtmosphere.GetTile(tile)!;

                for (var i = 0; i < tileAtmos.AdjacentTiles.Count; i++)
                {
                    var adjacentTile = tileAtmos.AdjacentTiles[i];

                    // TileAtmosphere has nullable disabled, so just in case...
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                    if (adjacentTile?.Air == null)
                        continue;

                    if (!includeBlocked)
                    {
                        var direction = (AtmosDirection) (1 << i);
                        if (tileAtmos.BlockedAirflow.IsFlagSet(direction))
                            continue;
                    }

                    if (invalidate)
                        adjacentTile.Invalidate();

                    yield return adjacentTile.Air;
                }
            }
        }

        #endregion

        #region Adjacent Update

        /// <summary>
        ///     Immediately updates a tile's blocked air directions.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        public void UpdateAdjacent(MapCoordinates coordinates)
        {
            if(TryGetGridAndTile(coordinates, out var tuple))
                UpdateAdjacent(tuple.Value.Grid, tuple.Value.Tile);
        }

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

            if (ComponentManager.TryGetComponent(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                gridAtmosphere.UpdateAdjacentBits(tile);
                return;
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
        public void HotspotExpose(MapCoordinates coordinates, float exposedTemperature, float exposedVolume, bool soh = false)
        {
            if(TryGetGridAndTile(coordinates, out var tuple))
                HotspotExpose(tuple.Value.Grid, tuple.Value.Tile, exposedTemperature, exposedVolume, soh);
        }

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

            if (ComponentManager.TryGetComponent(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                gridAtmosphere.GetTile(tile, false)?.HotspotExpose(exposedTemperature, exposedVolume, soh);
                gridAtmosphere.Invalidate(tile);
                return;
            }
        }

        #endregion

        #region Hotspot Extinguish

        /// <summary>
        ///     Extinguishes a hotspot (fire) on a certain tile, if any. Also invalidates the tile.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        public void HotspotExtinguish(MapCoordinates coordinates)
        {
            if(TryGetGridAndTile(coordinates, out var tuple))
                HotspotExtinguish(tuple.Value.Grid, tuple.Value.Tile);
        }

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

            if (ComponentManager.TryGetComponent(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                var tileAtmos = gridAtmosphere.GetTile(tile);

                if (tileAtmos != null)
                {
                    tileAtmos.Hotspot = new Hotspot();
                    gridAtmosphere.Invalidate(tile);
                }

                return;
            }
        }

        #endregion

        #region Hotspot Active

        /// <summary>
        ///     Returns whether there's an active hotspot (fire) on a certain tile.
        /// </summary>
        /// <param name="coordinates">Position where to get the tile.</param>
        /// <returns>Whether the hotspot is active or not.</returns>
        public bool IsHotspotActive(MapCoordinates coordinates)
        {
            if (TryGetGridAndTile(coordinates, out var tuple))
                return IsHotspotActive(tuple.Value.Grid, tuple.Value.Tile);

            return false;
        }

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

            if (ComponentManager.TryGetComponent(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                return gridAtmosphere.GetTile(tile, false)?.Hotspot.Valid ?? false;
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
        public bool IsTileMixtureProbablySafe(MapCoordinates coordinates)
        {
            return IsMixtureProbablySafe(GetTileMixture(coordinates));
        }

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
        public void FixVacuum(MapCoordinates coordinates)
        {
            if(TryGetGridAndTile(coordinates, out var tuple))
                FixVacuum(tuple.Value.Grid, tuple.Value.Tile);
        }

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

            if (ComponentManager.TryGetComponent(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                gridAtmosphere.FixVacuum(tile);

                return;
            }
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

        #endregion
    }
}
