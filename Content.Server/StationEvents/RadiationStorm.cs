using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.StationEvents;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Utility;
using JetBrains.Annotations;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.StationEvents
{
    [UsedImplicitly]
    public sealed class RadiationStorm : StationEvent
    {
        // Based on Goonstation style radiation storm with some TG elements (announcer, etc.)

        [Dependency] private IEntityManager _entityManager = default!;
        [Dependency] private IMapManager _mapManager = default!;
        [Dependency] private IRobustRandom _robustRandom = default!;

        public override string Name => "RadiationStorm";

        protected override string StartAnnouncement => Loc.GetString(
            "High levels of radiation detected near the station. Evacuate any areas containing abnormal green energy fields.");

        protected override string EndAnnouncement => Loc.GetString(
            "The radiation threat has passed. Please return to your workplaces.");

        /// <summary>
        /// How long until the radiation storm starts
        /// </summary>
        private const float StartupTime = 10;

        /// <summary>
        /// How long the radiation storm has been running for
        /// </summary>
        private float _timeElapsed;

        private int _pulsesRemaining;
        private float _timeUntilPulse;
        private const float MinPulseDelay = 0.2f;
        private const float MaxPulseDelay = 0.8f;

        public override void Startup()
        {
            base.Startup();
            EntitySystem.Get<AudioSystem>().PlayGlobal("/Audio/Announcements/radiation.ogg");
            IoCManager.InjectDependencies(this);

            _timeElapsed = 0.0f;
            _pulsesRemaining = _robustRandom.Next(30, 100);

            var componentManager = IoCManager.Resolve<IComponentManager>();

            foreach (var overlay in componentManager.EntityQuery<ServerOverlayEffectsComponent>())
            {
                overlay.AddOverlay(SharedOverlayID.RadiationPulseOverlay);
            }
        }

        public override void Shutdown()
        {
            base.Shutdown();

            // IOC uninject?
            _entityManager = null;
            _mapManager = null;
            _robustRandom = null;

            var componentManager = IoCManager.Resolve<IComponentManager>();

            foreach (var overlay in componentManager.EntityQuery<ServerOverlayEffectsComponent>())
            {
                overlay.RemoveOverlay(SharedOverlayID.RadiationPulseOverlay);
            }
        }

        public override void Update(float frameTime)
        {
            _timeElapsed += frameTime;

            if (_pulsesRemaining == 0)
            {
                Running = false;
            }

            if (!Running)
            {
                return;
            }

            if (_timeElapsed < StartupTime)
            {
                return;
            }

            _timeUntilPulse -= frameTime;

            if (_timeUntilPulse <= 0.0f)
            {
                // TODO: Probably rate-limit this for small grids (e.g. no more than 25% covered)
                foreach (var grid in _mapManager.GetAllGrids())
                {
                    if (grid.IsDefaultGrid) continue;
                    SpawnPulse(grid);
                }
            }
        }

        private void SpawnPulse(IMapGrid mapGrid)
        {
            var pulse = _entityManager.SpawnEntity("RadiationPulse", FindRandomGrid(mapGrid));
            pulse.GetComponent<RadiationPulseComponent>().DoPulse();
            _timeUntilPulse = _robustRandom.NextFloat() * (MaxPulseDelay - MinPulseDelay) + MinPulseDelay;
            _pulsesRemaining -= 1;
        }

        private EntityCoordinates FindRandomGrid(IMapGrid mapGrid)
        {
            // TODO: Need to get valid tiles? (maybe just move right if the tile we chose is invalid?)

            var randomX = _robustRandom.Next((int) mapGrid.WorldBounds.Left, (int) mapGrid.WorldBounds.Right);
            var randomY = _robustRandom.Next((int) mapGrid.WorldBounds.Bottom, (int) mapGrid.WorldBounds.Top);

            return mapGrid.ToCoordinates(randomX, randomY);
        }
    }
}
