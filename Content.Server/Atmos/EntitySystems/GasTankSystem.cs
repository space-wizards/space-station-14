using Content.Server.Explosion.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Cargo;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Configuration;
using Content.Shared.CCVar;

namespace Content.Server.Atmos.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasTankSystem : SharedGasTankSystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly ExplosionSystem _explosions = default!;
        [Dependency] private readonly SharedAudioSystem _audioSys = default!;
        [Dependency] private readonly UserInterfaceSystem _ui = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly ThrowingSystem _throwing = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        private const float TimerDelay = 0.5f;
        private float _timer;
        private const float MinimumSoundValvePressure = 10.0f;
        // TODO: FIX THIS
        private float _maxExplosionRange;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<GasTankComponent, EntParentChangedMessage>(OnParentChange);
            SubscribeLocalEvent<GasTankComponent, GasAnalyzerScanEvent>(OnAnalyzed);
            SubscribeLocalEvent<GasTankComponent, PriceCalculationEvent>(OnGasTankPrice);
            Subs.CVar(_cfg, CCVars.AtmosTankFragment, value => _maxExplosionRange = value, true);
        }

        public override void UpdateUserInterface(Entity<GasTankComponent> ent)
        {
            var (owner, component) = ent;
            _ui.SetUiState(owner,
                SharedGasTankUiKey.Key,
                new GasTankBoundUserInterfaceState
                {
                    TankPressure = component.Air.Pressure
                });
        }

        private void OnParentChange(EntityUid uid, GasTankComponent component, ref EntParentChangedMessage args)
        {
            // When an item is moved from hands -> pockets, the container removal briefly dumps the item on the floor.
            // So this is a shitty fix, where the parent check is just delayed. But this really needs to get fixed
            // properly at some point.
            component.CheckUser = true;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            _timer += frameTime;

            if (_timer < TimerDelay)
                return;

            _timer -= TimerDelay;

            var query = EntityQueryEnumerator<GasTankComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                var gasTank = (uid, comp);

                // If our gas tank is about to explode, continue
                // We do this step first to ensure that when we hit zero integrity we explode on the next tick, before we lose pressure
                if (!CheckStatus(gasTank))
                    continue;

                // Release gas if valve is open or as an emergency safety measure.
                if (comp.IsValveOpen || comp.Air.Pressure > comp.TankLeakPressure)
                    ReleaseGas(gasTank);

                if (comp.CheckUser)
                {
                    comp.CheckUser = false;
                    if (Transform(uid).ParentUid != comp.User)
                    {
                        DisconnectFromInternals(gasTank);
                    }
                }

                _atmosphereSystem.React(comp.Air, comp);

                if ((comp.IsConnected || comp.IsValveOpen) && _ui.IsUiOpen(uid, SharedGasTankUiKey.Key))
                {
                    UpdateUserInterface(gasTank);
                }
            }
        }

        /// <summary>
        /// Tries to release gas through the pressure release valve.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        private void ReleaseGas(Entity<GasTankComponent> entity)
        {
            if (entity.Comp.IsLowPressure)
                return;

            var environment = _atmosphereSystem.GetContainingMixture(entity.Owner, false, true);

            var deltaP = environment == null
                ? entity.Comp.Air.Pressure
                : entity.Comp.Air.Pressure - environment.Pressure;

            if (deltaP <= 0)
                return;

            // Turn deltaP into a more useful pressure value.
            var output = entity.Comp.Air.Pressure > entity.Comp.TankLeakPressure ? entity.Comp.MaxOutputPressure : entity.Comp.OutputPressure;
            deltaP = Math.Min(output, deltaP);
            var removed = RemoveAirPressure(entity, deltaP);

            if (environment != null)
                _atmosphereSystem.Merge(environment, removed);

            var strength = removed.Pressure * removed.Volume * Atmospherics.kPaToKg_m2;
            var dir = _random.NextAngle().ToWorldVec();
            _throwing.TryThrow(entity, dir * strength, strength);

            if (deltaP >= MinimumSoundValvePressure)
                _audioSys.PlayPvs(entity.Comp.RuptureSound, entity);
        }

        public double DischargeVolume(Entity<GasTankComponent> entity, float deltaP)
        {
            return TimerDelay * _atmosphereSystem.GetFlowVolume(entity.Comp.Air, deltaP, 0.01f);
        }

        public GasMixture RemoveAirPressure(Entity<GasTankComponent> gasTank, float pressure)
        {
            return RemoveAirAtPressure(gasTank, pressure, (float)DischargeVolume(gasTank, pressure));
        }

        public GasMixture RemoveAirAtPressure(Entity<GasTankComponent> gasTank, float pressure, float volume)
        {
            var molesNeeded = pressure * volume / (Atmospherics.R * gasTank.Comp.Air.Temperature);

            return RemoveAir(gasTank, molesNeeded);
        }

        public GasMixture RemoveAirOutput(Entity<GasTankComponent> gasTank, float volume)
        {
            var mixture = RemoveAirAtPressure(gasTank, gasTank.Comp.OutputPressure, volume);
            // We resize the volume because lungs breathe in volume rather than being pressure based atm.
            // If we don't do this, they won't consume all of the outputted gas or will consume way too much.
            mixture.Volume = volume;
            return mixture;
        }

        public GasMixture RemoveAir(Entity<GasTankComponent> gasTank, float amount)
        {
            return gasTank.Comp.Air.Remove(amount);
        }

        public void AssumeAir(Entity<GasTankComponent> ent, GasMixture giver)
        {
            _atmosphereSystem.Merge(ent.Comp.Air, giver);
            CheckStatus(ent);
        }

        public bool CheckStatus(Entity<GasTankComponent> ent)
        {
            var pressure = ent.Comp.Air.Pressure;
            if (ent.Comp.Integrity < 0)
            {
                // Give the gas a chance to build up more pressure.
                for (var i = 0; i < 3; i++)
                {
                    _atmosphereSystem.React(ent.Comp.Air, ent.Comp);
                }

                var environment = _atmosphereSystem.GetContainingMixture(ent.Owner, false, true);
                var deltaP = pressure;
                if (environment != null)
                {
                    _atmosphereSystem.Merge(environment, ent.Comp.Air);
                    deltaP = Math.Max(0f, pressure - environment.Pressure);
                }

                _audioSys.PlayPvs(ent.Comp.RuptureSound, Transform(ent).Coordinates, AudioParams.Default.WithVariation(0.125f));

                QueueDel(ent);

                // TODO: Put the magic number somewhere, maybe slow down the explosion by raising integrity and having
                _explosions.TriggerExplosive(ent, totalIntensity: deltaP * ent.Comp.Air.Volume / 100f);

                return false;
            }

            // Gas tank begins to fail.
            if (pressure > ent.Comp.TankLeakPressure)
            {
                ent.Comp.Integrity--;
                Dirty(ent);
            }
            else if (ent.Comp.Integrity < 4)
            {
                ent.Comp.Integrity++;
                Dirty(ent);
            }

            return true;
        }

        /// <summary>
        /// Returns the gas mixture for the gas analyzer
        /// </summary>
        private void OnAnalyzed(EntityUid uid, GasTankComponent component, GasAnalyzerScanEvent args)
        {
            args.GasMixtures ??= new List<(string, GasMixture?)>();
            args.GasMixtures.Add((Name(uid), component.Air));
        }

        private void OnGasTankPrice(EntityUid uid, GasTankComponent component, ref PriceCalculationEvent args)
        {
            args.Price += _atmosphereSystem.GetPrice(component.Air);
        }
    }
}
