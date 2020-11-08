using JetBrains.Annotations;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.StationEvents;
using Content.Server.Interfaces.GameTicking;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Utility;
using Robust.Server.Interfaces.Timing;
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
        [Dependency] private IRobustRandom _robustRandom = default!;

        public override string Name => "RadiationStorm";

        public override string StartAnnouncement => Loc.GetString(
            "High levels of radiation detected near the station. Evacuate any areas containing abnormal green energy fields.");

        protected override string EndAnnouncement => Loc.GetString(
            "The radiation threat has passed. Please return to your workplaces.");
        protected override string StartAudio => "/Audio/Announcements/radiation.ogg";

        protected override int StartWhen => 3;

        protected override int AnnounceWhen => 1;

        private float _timeUntilPulse;
        private const float MinPulseDelay = 0.2f;
        private const float MaxPulseDelay = 0.8f;

        private void ResetTimeUntilPulse()
        {
            _timeUntilPulse = _robustRandom.NextFloat() * (MaxPulseDelay - MinPulseDelay) + MinPulseDelay;
        }

        public override void Setup()
        {
            base.Setup();
            EndWhen = _robustRandom.Next(30, 80) + StartWhen; // We want to be forgiving about the radstorm.
        }

        public override void Start()
        {
            ResetTimeUntilPulse();

            var componentManager = IoCManager.Resolve<IComponentManager>();

            foreach (var overlay in componentManager.EntityQuery<ServerOverlayEffectsComponent>())
            {
                overlay.AddOverlay(SharedOverlayID.RadiationPulseOverlay);
            }
        }

        public override void End()
        {
            // IOC uninject?
            _entityManager = null;
            _robustRandom = null;

            var componentManager = IoCManager.Resolve<IComponentManager>();

            foreach (var overlay in componentManager.EntityQuery<ServerOverlayEffectsComponent>())
            {
                overlay.RemoveOverlay(SharedOverlayID.RadiationPulseOverlay);
            }
            base.End();
        }

        public override void Tick(float frameTime)
        {
            _timeUntilPulse -= frameTime;

            if (_timeUntilPulse <= 0.0f)
            {
                var pauseManager = IoCManager.Resolve<IPauseManager>();
                var gameTicker = IoCManager.Resolve<IGameTicker>();
                var defaultGrid = IoCManager.Resolve<IMapManager>().GetGrid(gameTicker.DefaultGridId);

                if (pauseManager.IsGridPaused(defaultGrid))
                    return;

                SpawnPulse(defaultGrid);
            }
        }

        private void SpawnPulse(IMapGrid mapGrid)
        {
            if (!TryFindRandomGrid(mapGrid, out var coordinates))
                return;

            var pulse = _entityManager.SpawnEntity("RadiationPulse", coordinates);
            pulse.GetComponent<RadiationPulseComponent>().DoPulse();
            ResetTimeUntilPulse();
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

            coordinates = mapGrid.ToCoordinates(randomX, randomY);

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
