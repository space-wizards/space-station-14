using Content.Server.Atmos.EntitySystems;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Configurations;
using Content.Shared.Atmos;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events
{
    internal sealed class GasLeak : StationEventSystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphere = default!;

        public override string Prototype => "GasLeak";

        private static readonly Gas[] LeakableGases =
        {
            Gas.Miasma,
            Gas.Plasma,
            Gas.Tritium,
            Gas.Frezon,
        };

        /// <summary>
        ///     Running cooldown of how much time until another leak.
        /// </summary>
        private float _timeUntilLeak;

        /// <summary>
        ///     How long between more gas being added to the tile.
        /// </summary>
        private const float LeakCooldown = 1.0f;


        // Event variables

        private EntityUid _targetStation;
        private EntityUid _targetGrid;
        private Vector2i _targetTile;
        private EntityCoordinates _targetCoords;
        private bool _foundTile;
        private Gas _leakGas;
        private float _molesPerSecond;
        private const int MinimumMolesPerSecond = 20;
        private float _endAfter = float.MaxValue;

        /// <summary>
        ///     Don't want to make it too fast to give people time to flee.
        /// </summary>
        private const int MaximumMolesPerSecond = 50;

        private const int MinimumGas = 250;
        private const int MaximumGas = 1000;
        private const float SparkChance = 0.05f;

        public override void Started()
        {
            base.Started();

            var mod = MathF.Sqrt(GetSeverityModifier());

            // Essentially we'll pick out a target amount of gas to leak, then a rate to leak it at, then work out the duration from there.
            if (TryFindRandomTile(out _targetTile, out _targetStation, out _targetGrid, out _targetCoords))
            {
                _foundTile = true;

                _leakGas = RobustRandom.Pick(LeakableGases);
                // Was 50-50 on using normal distribution.
                var totalGas = RobustRandom.Next(MinimumGas, MaximumGas) * mod;
                var startAfter = ((StationEventRuleConfiguration) Configuration).StartAfter;
                _molesPerSecond = RobustRandom.Next(MinimumMolesPerSecond, MaximumMolesPerSecond);
                _endAfter = totalGas / _molesPerSecond + startAfter;
                Sawmill.Info($"Leaking {totalGas} of {_leakGas} over {_endAfter - startAfter} seconds at {_targetTile}");
            }

            // Look technically if you wanted to guarantee a leak you'd do this in announcement but having the announcement
            // there just to fuck with people even if there is no valid tile is funny.
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (!RuleStarted)
                return;

            if (Elapsed > _endAfter)
            {
                ForceEndSelf();
                return;
            }

            _timeUntilLeak -= frameTime;

            if (_timeUntilLeak > 0f) return;
            _timeUntilLeak += LeakCooldown;

            if (!_foundTile ||
                _targetGrid == default ||
                EntityManager.Deleted(_targetGrid) ||
                !_atmosphere.IsSimulatedGrid(_targetGrid))
            {
                ForceEndSelf();
                return;
            }

            var environment = _atmosphere.GetTileMixture(_targetGrid, null, _targetTile, true);

            environment?.AdjustMoles(_leakGas, LeakCooldown * _molesPerSecond);
        }

        public override void Ended()
        {
            base.Ended();

            Spark();

            _foundTile = false;
            _targetGrid = default;
            _targetTile = default;
            _targetCoords = default;
            _leakGas = Gas.Oxygen;
            _endAfter = float.MaxValue;
        }

        private void Spark()
        {
            if (RobustRandom.NextFloat() <= SparkChance)
            {
                if (!_foundTile ||
                    _targetGrid == default ||
                    (!EntityManager.EntityExists(_targetGrid) ? EntityLifeStage.Deleted : EntityManager.GetComponent<MetaDataComponent>(_targetGrid).EntityLifeStage) >= EntityLifeStage.Deleted ||
                    !_atmosphere.IsSimulatedGrid(_targetGrid))
                {
                    return;
                }

                // Don't want it to be so obnoxious as to instantly murder anyone in the area but enough that
                // it COULD start potentially start a bigger fire.
                _atmosphere.HotspotExpose(_targetGrid, _targetTile, 700f, 50f, true);
                SoundSystem.Play("/Audio/Effects/sparks4.ogg", Filter.Pvs(_targetCoords), _targetCoords);
            }
        }
    }
}
