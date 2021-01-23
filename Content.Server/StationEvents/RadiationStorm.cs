using JetBrains.Annotations;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.Interfaces.GameTicking;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Utility;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.StationEvents
{
    [UsedImplicitly]
    public sealed class RadiationStorm : StationEvent
    {
        // Based on Goonstation style radiation storm with some TG elements (announcer, etc.)
        // Based on a more local Goonstation style radiation storm but using SS14 elements li

        [Dependency] private readonly IEntityManager _entityManager = default;
        [Dependency] private readonly IRobustRandom _robustRandom = default;

        [Dependency] private readonly IPauseManager _pauseManager = default;
        [Dependency] private readonly IGameTicker _gameTicker = default;
        [Dependency] private readonly IMapManager _mapManager = default;

        public override string Name => "RadiationStorm";
        public override string StartAnnouncement => Loc.GetString(
            "High levels of radiation detected near the station. Evacuate any areas containing abnormal green energy fields.");
        protected override string EndAnnouncement => Loc.GetString(
            "The radiation threat has passed. Please return to your workplaces.");
        public override string StartAudio => "/Audio/Announcements/radiation.ogg";
        protected override float StartAfter => 10.0f;

        /// <summary>
        /// How long until the next pulse.
        /// </summary>
        private float _timeUntilPulse;

        /// <summary>
        /// Minimum pulse delay between pulses.
        /// </summary>
        private const float MinPulseDelay = 0.5f;
        /// <summary>
        /// Maximum pulse delay between pulses.
        /// </summary>
        private const float MaxPulseDelay = 2.0f;

        /// <summary>
        /// Map grid where the event takes place.
        /// </summary>
        private IMapGrid _eventMapGrid;

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
            _eventMapGrid = _mapManager.GetGrid(_gameTicker.DefaultGridId);
            ResetTimeUntilPulse();
            base.Startup();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (!Started || !Running) return;

            _timeUntilPulse -= frameTime;

            if (_timeUntilPulse <= 0.0f)
            {
                if (_pauseManager.IsGridPaused(_eventMapGrid))
                {
                    return;
                }
                SpawnRadiationAnomaly();
                ResetTimeUntilPulse();
            }
        }

        private void SpawnRadiationAnomaly()
        {
            if (!TryFindRandomGrid(_eventMapGrid, out var coordinates))
                return;
            _entityManager.SpawnEntity("RadiationPulse", coordinates);
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

            coordinates = _eventMapGrid.ToCoordinates(randomX + 0.5f, randomY + 0.5f);

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
