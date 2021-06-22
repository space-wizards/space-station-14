using System;
using System.Diagnostics.CodeAnalysis;
using Content.Server.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Atmos.EntitySystems
{
    [UsedImplicitly]
    public partial class AtmosphereSystem : SharedAtmosphereSystem
    {
        [Dependency] private readonly IPrototypeManager _protoMan = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPauseManager _pauseManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        private GridTileLookupSystem? _gridTileLookup = null;

        private const float ExposedUpdateDelay = 1f;
        private float _exposedTimer = 0f;

        public GridTileLookupSystem GridTileLookupSystem => _gridTileLookup ??= Get<GridTileLookupSystem>();

        public override void Initialize()
        {
            base.Initialize();

            InitializeGases();
            InitializeCVars();

            #region Events

            // Map events.
            _mapManager.MapCreated += OnMapCreated;
            _mapManager.TileChanged += OnTileChanged;

            #endregion
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _mapManager.MapCreated -= OnMapCreated;
            _mapManager.TileChanged -= OnTileChanged;
        }

        private void OnTileChanged(object? sender, TileChangedEventArgs eventArgs)
        {
            // When a tile changes, we want to update it only if it's gone from
            // space -> not space or vice versa. So if the old tile is the
            // same as the new tile in terms of space-ness, ignore the change

            if (eventArgs.NewTile.IsSpace() == eventArgs.OldTile.IsSpace())
            {
                return;
            }

            GetGridAtmosphere(eventArgs.NewTile.GridIndex)?.Invalidate(eventArgs.NewTile.GridIndices);
        }

        private void OnMapCreated(object? sender, MapEventArgs e)
        {
            if (e.Map == MapId.Nullspace)
                return;

            var map = _mapManager.GetMapEntity(e.Map);

            if (!map.HasComponent<IGridAtmosphereComponent>())
                map.AddComponent<SpaceGridAtmosphereComponent>();
        }

        #region Helper Methods
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
        ///     Unlike <see cref="GetGridAtmosphere"/>, this doesn't return space grid when not found.
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

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            UpdateProcessing(frameTime);

            _exposedTimer += frameTime;

            if (_exposedTimer >= ExposedUpdateDelay)
            {
                foreach (var exposed in EntityManager.ComponentManager.EntityQuery<AtmosExposedComponent>(true))
                {
                    var tile = exposed.Owner.Transform.Coordinates.GetTileAtmosphere();
                    if (tile == null) continue;
                    exposed.Update(tile, _exposedTimer);
                }

                _exposedTimer -= ExposedUpdateDelay;
            }
        }
    }
}
