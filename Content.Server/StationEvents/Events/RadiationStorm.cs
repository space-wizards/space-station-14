using Content.Server.GameTicking;
using Content.Server.Radiation;
using Content.Shared.Coordinates;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.StationEvents.Events
{
    [UsedImplicitly]
    public sealed class RadiationStorm : StationEvent
    {
        // Based on Goonstation style radiation storm with some TG elements (announcer, etc.)

        [Dependency] private IEntityManager _entityManager = default!;
        [Dependency] private IRobustRandom _robustRandom = default!;

        public override string Name => "RadiationStorm";
        public override string StartAnnouncement => Loc.GetString("station-event-radiation-storm-start-announcement");
        protected override string EndAnnouncement => Loc.GetString("station-event-radiation-storm-end-announcement");
        public override string StartAudio => "/Audio/Announcements/radiation.ogg";
        protected override float StartAfter => 10.0f;

        // Event specific details
        private float _timeUntilPulse;
        private const float MinPulseDelay = 0.2f;
        private const float MaxPulseDelay = 0.8f;

        private void ResetTimeUntilPulse()
        {
            _timeUntilPulse = _robustRandom.NextFloat() * (MaxPulseDelay - MinPulseDelay) + MinPulseDelay;
        }

        public override void Announce()
        {
            base.Announce();
            EndAfter = _robustRandom.Next(30, 80) + StartAfter; // We want to be forgiving about the radstorm.
        }

        public override void Startup()
        {
            ResetTimeUntilPulse();
            base.Startup();
        }

        public override void Shutdown()
        {
            base.Shutdown();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (!Started || !Running) return;

            _timeUntilPulse -= frameTime;

            if (_timeUntilPulse <= 0.0f)
            {
                var pauseManager = IoCManager.Resolve<IPauseManager>();
                var defaultGrid = IoCManager.Resolve<IMapManager>().GetGrid(EntitySystem.Get<GameTicker>().DefaultGridId);

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

            var bounds = mapGrid.LocalBounds;
            var randomX = _robustRandom.Next((int) bounds.Left, (int) bounds.Right);
            var randomY = _robustRandom.Next((int) bounds.Bottom, (int) bounds.Top);

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
