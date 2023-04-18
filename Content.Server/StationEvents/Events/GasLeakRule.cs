using Content.Server.Atmos.EntitySystems;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;
using Content.Shared.Atmos;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events
{
    internal sealed class GasLeakRule : StationEventSystem<GasLeakRuleComponent>
    {
        [Dependency] private readonly AtmosphereSystem _atmosphere = default!;

        protected override void Started(EntityUid uid, GasLeakRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
        {
            base.Started(uid, component, gameRule, args);

            if (!TryComp<StationEventComponent>(uid, out var stationEvent))
                return;

            var mod = MathF.Sqrt(GetSeverityModifier());

            // Essentially we'll pick out a target amount of gas to leak, then a rate to leak it at, then work out the duration from there.
            if (TryFindRandomTile(out component._targetTile, out component._targetStation, out component._targetGrid, out component._targetCoords))
            {
                component._foundTile = true;

                component._leakGas = RobustRandom.Pick(component.LeakableGases);
                // Was 50-50 on using normal distribution.
                var totalGas = RobustRandom.Next(component.MinimumGas, component.MaximumGas) * mod;
                var startAfter = stationEvent.StartDelay;
                component._molesPerSecond = RobustRandom.Next(component.MinimumMolesPerSecond, component.MaximumMolesPerSecond);

                //todo this doesn't actually work
                //stationEvent.Duration = totalGas / component._molesPerSecond + startAfter;
                Sawmill.Info($"Leaking {totalGas} of {component._leakGas} over {component._endAfter - startAfter} seconds at {component._targetTile}");
            }

            // Look technically if you wanted to guarantee a leak you'd do this in announcement but having the announcement
            // there just to fuck with people even if there is no valid tile is funny.
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            //todo clusterfuck
            var query = EntityQueryEnumerator<GasLeakRuleComponent, StationEventComponent, GameRuleComponent>();
            while (query.MoveNext(out var uid, out var gasLeak, out var stationEvent, out var gameRule))
            {
                /*
                if (stationEvent.Elapsed > _endAfter)
                {
                    ForceEndSelf();
                    return;
                }

                _timeUntilLeak -= frameTime;

                if (_timeUntilLeak > 0f)
                    return;
                _timeUntilLeak += LeakCooldown;

                if (!_foundTile ||
                    _targetGrid == default ||
                    Deleted(_targetGrid) ||
                    !_atmosphere.IsSimulatedGrid(_targetGrid))
                {
                    ForceEndSelf();
                    return;
                }

                var environment = _atmosphere.GetTileMixture(_targetGrid, null, _targetTile, true);

                environment?.AdjustMoles(_leakGas, LeakCooldown * _molesPerSecond);*/
            }
        }

        protected override void Ended(EntityUid uid, GasLeakRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
        {
            base.Ended(uid, component, gameRule, args);
            Spark(uid, component);
        }

        private void Spark(EntityUid uid, GasLeakRuleComponent component)
        {
            if (RobustRandom.NextFloat() <= component.SparkChance)
            {
                if (!component._foundTile ||
                    component._targetGrid == default ||
                    (!Exists(component._targetGrid) ? EntityLifeStage.Deleted : MetaData(component._targetGrid).EntityLifeStage) >= EntityLifeStage.Deleted ||
                    !_atmosphere.IsSimulatedGrid(component._targetGrid))
                {
                    return;
                }

                // Don't want it to be so obnoxious as to instantly murder anyone in the area but enough that
                // it COULD start potentially start a bigger fire.
                _atmosphere.HotspotExpose(component._targetGrid, component._targetTile, 700f, 50f, null, true);
                Audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/sparks4.ogg"), component._targetCoords);
            }
        }
    }
}
