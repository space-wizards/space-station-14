using Content.Server.GameObjects.Components.Atmos;
using Content.Server.Interfaces.GameTicking;
using Content.Shared.Atmos;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Random;

#nullable enable
namespace Content.Server.StationEvents
{
    internal sealed class GasLeak : StationEvent
    {
        public override string Name => "GasLeak";

        public override string? StartAnnouncement =>
            "Attention crew, there is a gas leak on the station. We advise you to avoid the area and wear suit internals in the meantime.";

        // Sourced from https://github.com/vgstation-coders/vgstation13/blob/2c5a491446ab824a8fbbf39bcf656b590e0228df/sound/misc/bloblarm.ogg
        public override string? StartAudio => "/Audio/Announcements/bloblarm.ogg";

        protected override string? EndAnnouncement => "The source of the gas leak has been fixed. Please be cautious around areas with gas remaining.";

        private static readonly Gas[] LeakableGases = {
            Gas.Plasma,
            Gas.Tritium,
        };

        public override int EarliestStart => 10;

        public override int MinimumPlayers => 5;

        /// <summary>
        ///     Give people time to get their internals on.
        /// </summary>
        protected override float StartAfter => 20f;

        /// <summary>
        ///     Don't know how long the event will be until we calculate the leak amount.
        /// </summary>
        protected override float EndAfter { get; set; } = float.MaxValue;

        /// <summary>
        ///     Running cooldown of how much time until another leak.
        /// </summary>
        private float _timeUntilLeak;

        /// <summary>
        ///     How long between more gas being added to the tile.
        /// </summary>
        private const float LeakCooldown = 1.0f;

        // Event variables

        private IEntity? _targetGrid;

        private Vector2i _targetTile;

        private EntityCoordinates _targetCoords;

        private bool _foundTile;

        private Gas _leakGas;

        private float _molesPerSecond;

        private const int MinimumMolesPerSecond = 20;

        /// <summary>
        ///     Don't want to make it too fast to give people time to flee.
        /// </summary>
        private const int MaximumMolesPerSecond = 50;

        private const int MinimumGas = 250;

        private const int MaximumGas = 1000;

        private const float SparkChance = 0.05f;

        public override void Startup()
        {
            base.Startup();
            var robustRandom = IoCManager.Resolve<IRobustRandom>();

            // Essentially we'll pick out a target amount of gas to leak, then a rate to leak it at, then work out the duration from there.
            if (TryFindRandomTile(out _targetTile, robustRandom))
            {
                _foundTile = true;

                _leakGas = robustRandom.Pick(LeakableGases);
                // Was 50-50 on using normal distribution.
                var totalGas = (float) robustRandom.Next(MinimumGas, MaximumGas);
                _molesPerSecond = robustRandom.Next(MinimumMolesPerSecond, MaximumMolesPerSecond);
                 EndAfter = totalGas / _molesPerSecond + StartAfter;
                 Logger.InfoS("stationevents", $"Leaking {totalGas} of {_leakGas} over {EndAfter - StartAfter} seconds at {_targetTile}");
            }

            // Look technically if you wanted to guarantee a leak you'd do this in announcement but having the announcement
            // there just to fuck with people even if there is no valid tile is funny.
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (!Started || !Running) return;

            _timeUntilLeak -= frameTime;

            if (_timeUntilLeak > 0f) return;
            _timeUntilLeak += LeakCooldown;

            if (!_foundTile ||
                _targetGrid == null ||
                _targetGrid.Deleted ||
                !_targetGrid.TryGetComponent(out GridAtmosphereComponent? gridAtmos))
            {
                Running = false;
                return;
            }

            var atmos = gridAtmos.GetTile(_targetTile);

            atmos?.Air?.AdjustMoles(_leakGas, LeakCooldown * _molesPerSecond);
            atmos?.Invalidate();
        }

        public override void Shutdown()
        {
            base.Shutdown();

            Spark();

            _foundTile = false;
            _targetGrid = null;
            _targetTile = default;
            _targetCoords = default;
            _leakGas = Gas.Oxygen;
            EndAfter = float.MaxValue;
        }

        private void Spark()
        {
            var robustRandom = IoCManager.Resolve<IRobustRandom>();
            if (robustRandom.NextFloat() <= SparkChance)
            {
                if (!_foundTile ||
                    _targetGrid == null ||
                    _targetGrid.Deleted ||
                    !_targetGrid.TryGetComponent(out GridAtmosphereComponent? gridAtmos))
                {
                    return;
                }

                var atmos = gridAtmos.GetTile(_targetTile);
                // Don't want it to be so obnoxious as to instantly murder anyone in the area but enough that
                // it COULD start potentially start a bigger fire.
                atmos?.HotspotExpose(700f, 50f, true);
                EntitySystem.Get<AudioSystem>().PlayAtCoords("/Audio/Effects/sparks4.ogg", _targetCoords);
            }
        }

        private bool TryFindRandomTile(out Vector2i tile, IRobustRandom? robustRandom = null)
        {
            tile = default;
            var defaultGridId = IoCManager.Resolve<IGameTicker>().DefaultGridId;

            if (!IoCManager.Resolve<IMapManager>().TryGetGrid(defaultGridId, out var grid) ||
                !IoCManager.Resolve<IEntityManager>().TryGetEntity(grid.GridEntityId, out _targetGrid)) return false;

            _targetGrid.EnsureComponent(out GridAtmosphereComponent gridAtmos);
            robustRandom ??= IoCManager.Resolve<IRobustRandom>();
            var found = false;
            var gridBounds = grid.WorldBounds;
            var gridPos = grid.WorldPosition;

            for (var i = 0; i < 10; i++)
            {
                var randomX = robustRandom.Next((int) gridBounds.Left, (int) gridBounds.Right);
                var randomY = robustRandom.Next((int) gridBounds.Bottom, (int) gridBounds.Top);

                tile = new Vector2i(randomX - (int) gridPos.X, randomY - (int) gridPos.Y);
                if (gridAtmos.IsSpace(tile) || gridAtmos.IsAirBlocked(tile)) continue;
                found = true;
                _targetCoords = grid.GridTileToLocal(tile);
                break;
            }

            if (!found) return false;

            return true;
        }
    }
}
