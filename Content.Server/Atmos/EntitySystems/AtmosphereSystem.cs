using Content.Server.Atmos.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Atmos.EntitySystems
{
    [UsedImplicitly]
    public partial class AtmosphereSystem : SharedAtmosphereSystem
    {
        [Dependency] private readonly IPrototypeManager _protoMan = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        private const float ExposedUpdateDelay = 1f;
        private float _exposedTimer = 0f;

        public override void Initialize()
        {
            base.Initialize();

            UpdatesAfter.Add(typeof(NodeGroupSystem));

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

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            UpdateProcessing(frameTime);

            _exposedTimer += frameTime;

            if (_exposedTimer >= ExposedUpdateDelay)
            {
                foreach (var exposed in EntityManager.ComponentManager.EntityQuery<AtmosExposedComponent>())
                {
                    // TODO ATMOS: Kill this with fire.
                    var atmos = GetGridAtmosphere(exposed.Owner.Transform.Coordinates);
                    var tile = atmos.GetTile(exposed.Owner.Transform.Coordinates);
                    if (tile == null) continue;
                    exposed.Update(tile, _exposedTimer, this);
                }

                _exposedTimer -= ExposedUpdateDelay;
            }
        }
    }
}
