using Content.Server.Administration.Logs;
using Content.Server.Atmos.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Map;

namespace Content.Server.Atmos.EntitySystems
{
    /// <summary>
    ///     This is our SSAir equivalent, if you need to interact with or query atmos in any way, go through this.
    /// </summary>
    [UsedImplicitly]
    public sealed partial class AtmosphereSystem : SharedAtmosphereSystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly AdminLogSystem _adminLog = default!;
        [Dependency] private readonly SharedContainerSystem _containers = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;

        private const float ExposedUpdateDelay = 1f;
        private float _exposedTimer = 0f;

        public override void Initialize()
        {
            base.Initialize();

            UpdatesAfter.Add(typeof(NodeGroupSystem));

            InitializeGases();
            InitializeCommands();
            InitializeCVars();
            InitializeGrid();

            #region Events

            // Map events.
            _mapManager.TileChanged += OnTileChanged;

            #endregion
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _mapManager.TileChanged -= OnTileChanged;

            ShutdownCommands();
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

            InvalidateTile(eventArgs.NewTile.GridIndex, eventArgs.NewTile.GridIndices);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            UpdateProcessing(frameTime);
            UpdateHighPressure(frameTime);

            _exposedTimer += frameTime;

            if (_exposedTimer < ExposedUpdateDelay)
                return;

            foreach (var (exposed, transform) in EntityManager.EntityQuery<AtmosExposedComponent, TransformComponent>())
            {
                // Used for things like disposals/cryo to change which air people are exposed to.
                var airEvent = new AtmosExposedGetAirEvent();
                RaiseLocalEvent(exposed.Owner, ref airEvent, false);

                airEvent.Gas ??= GetTileMixture(transform.Coordinates);
                if (airEvent.Gas == null)
                    continue;

                var updateEvent = new AtmosExposedUpdateEvent(transform.Coordinates, airEvent.Gas);
                RaiseLocalEvent(exposed.Owner, ref updateEvent);
            }

            _exposedTimer -= ExposedUpdateDelay;
        }
    }
}
