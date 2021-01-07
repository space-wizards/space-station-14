using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.StationEvents;
using Content.Server.Interfaces.GameTicking;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Utility;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Random;

namespace Content.Server.StationEvents
{
    [UsedImplicitly]
    public sealed class RadiationStorm : StationEvent
    {
        // Based on Goonstation style radiation storm with some TG elements (announcer, etc.)

        [Dependency] private IEntityManager _entityManager = default!;
        [Dependency] private IRobustRandom _robustRandom = default!;

        public override string Name => "RadiationStorm";

        protected override string StartAnnouncement => Loc.GetString(
            "High levels of radiation detected near the station. Evacuate any areas containing abnormal green energy fields.");

        protected override string EndAnnouncement => Loc.GetString(
            "The radiation threat has passed. Please return to your workplaces.");

        /// <summary>
        /// How long until the radiation storm starts
        /// </summary>
        private const float StartupTime = 5;

        /// <summary>
        /// How long the radiation storm has been running for
        /// </summary>
        private float _timeElapsed;

        private int _pulsesRemaining;
        private float _timeUntilPulse;
        private const float MinPulseDelay = 0.5f;
        private const float MaxPulseDelay = 2.0f;

        private void ResetTimeUntilPulse()
        {
            _timeUntilPulse = _robustRandom.NextFloat() * (MaxPulseDelay - MinPulseDelay) + MinPulseDelay;
        }

        public override void Startup()
        {
            base.Startup();
            EntitySystem.Get<AudioSystem>().PlayGlobal("/Audio/Announcements/radiation.ogg");
            IoCManager.InjectDependencies(this);

            ResetTimeUntilPulse();
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
                var pauseManager = IoCManager.Resolve<IPauseManager>();
                var gameTicker = IoCManager.Resolve<IGameTicker>();
                var defaultGrid = IoCManager.Resolve<IMapManager>().GetGrid(gameTicker.DefaultGridId);

                if (pauseManager.IsGridPaused(defaultGrid))
                    return;

                SpawnPulseWithLight(defaultGrid);
            }
        }

        private void SpawnPulseWithLight(IMapGrid mapGrid)
        {
            if (!TryFindRandomGrid(mapGrid, out var coordinates))
                return;

            var pulse = _entityManager.SpawnEntity("RadiationPulse", coordinates);
            if (pulse.TryGetComponent(out RadiationPulseComponent radPulse))
            {
                var light = pulse.AddComponent<PointLightComponent>();
                light.Color = new Color(0.0f, 1.0f, 0.0f, 1.0f);
                light.Radius = radPulse.Range;

                radPulse.DoPulse();
            }
            ResetTimeUntilPulse();
            _pulsesRemaining--;
        }

        private bool TryFindRandomGrid(IMapGrid mapGrid, out EntityCoordinates coordinates)
        {
            if (!mapGrid.Index.IsValid())
            {
                coordinates = default;
                return false;
            }
            var randomX = _robustRandom.Next((int) mapGrid.WorldBounds.Left, (int) mapGrid.WorldBounds.Right);
            var randomY = _robustRandom.Next((int) mapGrid.WorldBounds.Bottom, (int) mapGrid.WorldBounds.Top);

            coordinates = mapGrid.ToCoordinates(randomX + 0.5f, randomY + 0.5f);

            // TODO: Need to get valid tiles? (maybe just move right if the tile we chose is invalid?)
            if (!coordinates.IsValid(_entityManager))
            {
                coordinates = default;
                return false;
            }
            return true;
        }
    }
}
