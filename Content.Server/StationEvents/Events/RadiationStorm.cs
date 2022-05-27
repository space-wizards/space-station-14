using Content.Server.Radiation;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Coordinates;
using Content.Shared.Sound;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events
{
    [UsedImplicitly]
    public sealed class RadiationStorm : StationEvent
    {
        // Based on Goonstation style radiation storm with some TG elements (announcer, etc.)

        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        private StationSystem _stationSystem = default!;

        public override string Name => "RadiationStorm";
        public override string StartAnnouncement => Loc.GetString("station-event-radiation-storm-start-announcement");
        protected override string EndAnnouncement => Loc.GetString("station-event-radiation-storm-end-announcement");
        public override SoundSpecifier? StartAudio => new SoundPathSpecifier("/Audio/Announcements/radiation.ogg");
        protected override float StartAfter => 10.0f;

        // Event specific details
        private float _timeUntilPulse;
        private const float MinPulseDelay = 0.2f;
        private const float MaxPulseDelay = 0.8f;
        private EntityUid _target = EntityUid.Invalid;

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
            _entityManager.EntitySysManager.Resolve(ref _stationSystem);
            ResetTimeUntilPulse();

            if (_stationSystem.Stations.Count == 0)
            {
                Running = false;
                return;
            }

            _target = _robustRandom.Pick(_stationSystem.Stations);
            base.Startup();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (!Started || !Running) return;
            if (_target.Valid == false)
            {
                Running = false;
                return;
            }

            _timeUntilPulse -= frameTime;

            if (_timeUntilPulse <= 0.0f)
            {
                var mapManager = IoCManager.Resolve<IMapManager>();
                // Account for split stations by just randomly picking a piece of it.
                var possibleTargets = _entityManager.GetComponent<StationDataComponent>(_target).Grids;
                if (possibleTargets.Count == 0)
                {
                    Running = false;
                    return;
                }

                var stationEnt = _robustRandom.Pick(possibleTargets);

                if (!_entityManager.TryGetComponent<IMapGridComponent>(stationEnt, out var grid))
                    return;

                if (mapManager.IsGridPaused(grid.GridIndex))
                    return;

                SpawnPulse(grid.Grid);
            }
        }

        private void SpawnPulse(IMapGrid mapGrid)
        {
            if (!TryFindRandomGrid(mapGrid, out var coordinates))
                return;

            var pulse = _entityManager.SpawnEntity("RadiationPulse", coordinates);
            _entityManager.GetComponent<RadiationPulseComponent>(pulse).DoPulse();
            ResetTimeUntilPulse();
        }

        public void SpawnPulseAt(EntityCoordinates at)
        {
            var pulse = IoCManager.Resolve<IEntityManager>()
                .SpawnEntity("RadiationPulse", at);
            _entityManager.GetComponent<RadiationPulseComponent>(pulse).DoPulse();
        }

        private bool TryFindRandomGrid(IMapGrid mapGrid, out EntityCoordinates coordinates)
        {
            if (!mapGrid.Index.IsValid())
            {
                coordinates = default;
                return false;
            }

            var bounds = mapGrid.LocalAABB;
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
