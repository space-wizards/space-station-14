using System;
using System.Diagnostics.CodeAnalysis;
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

            if (_mapManager.GetMapEntity(coordinates.MapId).TryGetComponent(out IGridAtmosphereComponent? atmosMap)
                && atmosMap.Simulated)
            {
                atmosphere = atmosMap;
                return true;
            }

            atmosphere = null;
            return false;
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

        #region Active Tiles

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

        #region Update Adjacent

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

        #region Hotspots

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

        /// <summary>
        ///     Extinguishes a hotspot (fire) on a certain tile, if any. Also invalidates the tile.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        public void ExtinguishHotspot(MapCoordinates coordinates)
        {
            if(TryGetGridAndTile(coordinates, out var tuple))
                ExtinguishHotspot(tuple.Value.Grid, tuple.Value.Tile);
        }

        /// <summary>
        ///     Extinguishes a hotspot (fire) on a certain tile, if any. Also invalidates the tile.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        public void ExtinguishHotspot(EntityCoordinates coordinates)
        {
            if(TryGetGridAndTile(coordinates, out var tuple))
                ExtinguishHotspot(tuple.Value.Grid, tuple.Value.Tile);
        }

        /// <summary>
        ///     Extinguishes a hotspot (fire) on a certain tile, if any. Also invalidates the tile.
        /// </summary>
        /// <param name="grid">Grid where to get the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        public void ExtinguishHotspot(GridId grid, Vector2i tile)
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

        private bool TryGetGridAndTile(MapCoordinates coordinates, [NotNullWhen(true)] out (GridId Grid, Vector2i Tile)? tuple)
        {
            if (!_mapManager.TryFindGridAt(coordinates, out var grid))
            {
                tuple = null;
                return false;
            }

            tuple = (grid.Index, grid.TileIndicesFor(coordinates));
            return true;
        }

        private bool TryGetGridAndTile(EntityCoordinates coordinates, [NotNullWhen(true)] out (GridId Grid, Vector2i Tile)? tuple)
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
