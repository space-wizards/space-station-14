using Content.Shared.Damage;
using Content.Shared.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Temperature.Systems;
using Content.Server.Body.Components;
using Content.Shared.Examine;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Rejuvenate;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Atmos.Miasma
{
    public sealed class MiasmaSystem : EntitySystem
    {
        [Dependency] private readonly TransformSystem _transformSystem = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;
        [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;

        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        /// System Variables

        /// Rotting

        /// <summary>
        /// How often the rotting ticks.
        /// Feel free to weak this if there are perf concerns.
        /// </summary>
        private float _rotUpdateRate = 5f;

        /// Miasma Disease Pool
        /// Miasma outbreaks are not per-entity,
        /// so this ensures that each entity in the same incident
        /// receives the same disease.

        public readonly IReadOnlyList<string> MiasmaDiseasePool = new[]
        {
            "VentCough",
            "AMIV",
            "SpaceCold",
            "SpaceFlu",
            "BirdFlew",
            "VanAusdallsRobovirus",
            "BleedersBite",
            "Plague",
            "TongueTwister",
            "MemeticAmirmir"
        };

        /// <summary>
        /// The current pool disease.
        /// </summary>
        private string _poolDisease = "";

        /// <summary>
        /// The list of diseases in the pool.
        /// </summary>

        /// <summary>
        /// The target time it waits until..
        /// After that, it resets current time + _poolRepickTime.
        /// Any infection will also reset it to current time + _poolRepickTime.
        /// </summary>
        private TimeSpan _diseaseTime = TimeSpan.FromMinutes(5);

        /// <summary>
        /// How long without an infection before we pick a new disease.
        /// </summary>
        private TimeSpan _poolRepickTime = TimeSpan.FromMinutes(5);

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            // Disease pool

            if (_timing.CurTime >= _diseaseTime)
            {
                _diseaseTime = _timing.CurTime + _poolRepickTime;
                _poolDisease = _random.Pick(MiasmaDiseasePool);
            }

            // Rotting
            foreach (var (rotting, perishable, metadata) in EntityQuery<RottingComponent, PerishableComponent, MetaDataComponent>())
            {
                if (!perishable.Progressing)
                    continue;

                if (!IsRotting(perishable, metadata))
                    continue;

                if (_timing.CurTime < perishable.RotNextUpdate) // This is where it starts to get noticable on larger animals, no need to run every second
                    continue;

                perishable.RotNextUpdate = _timing.CurTime + TimeSpan.FromSeconds(_rotUpdateRate);

                EnsureComp<FliesComponent>(perishable.Owner);

                if (rotting.DealDamage)
                {
                    DamageSpecifier damage = new();
                    damage.DamageDict.Add("Blunt", 0.3); // Slowly accumulate enough to gib after like half an hour
                    damage.DamageDict.Add("Cellular", 0.3); // Cloning rework might use this eventually

                    _damageableSystem.TryChangeDamage(perishable.Owner, damage, true, true, origin: perishable.Owner);
                }

                if (!TryComp<PhysicsComponent>(perishable.Owner, out var physics))
                    continue;
                // We need a way to get the mass of the mob alone without armor etc in the future

                float molRate = perishable.MolsPerSecondPerUnitMass * _rotUpdateRate;

                var transform = Transform(perishable.Owner);
                var indices = _transformSystem.GetGridOrMapTilePosition(perishable.Owner);

                var tileMix = _atmosphereSystem.GetTileMixture(transform.GridUid, null, indices, true);
                tileMix?.AdjustMoles(Gas.Miasma, molRate * physics.FixturesMass);
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            // Core rotting stuff
            SubscribeLocalEvent<RottingComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<RottingComponent, OnTemperatureChangeEvent>(OnTempChange);
            SubscribeLocalEvent<PerishableComponent, MobStateChangedEvent>(OnMobStateChanged);
            SubscribeLocalEvent<PerishableComponent, BeingGibbedEvent>(OnGibbed);
            SubscribeLocalEvent<PerishableComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<RottingComponent, RejuvenateEvent>(OnRejuvenate);
            // Containers
            SubscribeLocalEvent<AntiRottingContainerComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
            SubscribeLocalEvent<AntiRottingContainerComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
            // Fly audiovisual stuff
            SubscribeLocalEvent<FliesComponent, ComponentInit>(OnFliesInit);
            SubscribeLocalEvent<FliesComponent, ComponentShutdown>(OnFliesShutdown);

            // Init disease pool
            _poolDisease = _random.Pick(MiasmaDiseasePool);
        }

        private void OnShutdown(EntityUid uid, RottingComponent component, ComponentShutdown args)
        {
            RemComp<FliesComponent>(uid);
            if (TryComp<PerishableComponent>(uid, out var perishable))
            {
                perishable.TimeOfDeath = TimeSpan.Zero;
                perishable.RotNextUpdate = TimeSpan.Zero;
            }
        }

        private void OnTempChange(EntityUid uid, RottingComponent component, OnTemperatureChangeEvent args)
        {
            if (HasComp<BodyPreservedComponent>(uid))
                return;
            bool decompose = (args.CurrentTemperature > Atmospherics.T0C + 0.85f);
            ToggleDecomposition(uid, decompose);
        }

        private void OnMobStateChanged(EntityUid uid, PerishableComponent component, MobStateChangedEvent args)
        {
            if (_mobState.IsDead(uid))
            {
                EnsureComp<RottingComponent>(uid);
                component.TimeOfDeath = _timing.CurTime;
            }
        }

        /// <summary>
        ///     Has enough time passed for <paramref name="perishable"/> to start rotting?
        /// </summary>
        private bool IsRotting(PerishableComponent perishable, MetaDataComponent? metadata = null)
        {
            if (perishable.TimeOfDeath == TimeSpan.Zero)
                return false;

            if (_timing.CurTime >= perishable.TimeOfDeath + perishable.RotAfter + _metaDataSystem.GetPauseTime(perishable.Owner, metadata))
                return true;

            return false;
        }

        private void OnGibbed(EntityUid uid, PerishableComponent component, BeingGibbedEvent args)
        {
            if (!TryComp<PhysicsComponent>(uid, out var physics))
                return;

            if (!IsRotting(component))
                return;

            var molsToDump = (component.MolsPerSecondPerUnitMass * physics.FixturesMass) * (float)(_timing.CurTime - component.TimeOfDeath).TotalSeconds;
            var transform = Transform(uid);
            var indices = _transformSystem.GetGridOrMapTilePosition(uid, transform);
            var tileMix = _atmosphereSystem.GetTileMixture(transform.GridUid, null, indices, true);
            tileMix?.AdjustMoles(Gas.Miasma, molsToDump);

            // Waste of entities to let these through
            foreach (var part in args.GibbedParts)
                EntityManager.DeleteEntity(part);
        }

        private void OnExamined(EntityUid uid, PerishableComponent component, ExaminedEvent args)
        {
            if (!IsRotting(component))
                return;

            var stage = (_timing.CurTime - component.TimeOfDeath).TotalSeconds / component.RotAfter.TotalSeconds;
            var description = stage switch {
                >= 3 => "miasma-extremely-bloated",
                >= 2 => "miasma-bloated",
                   _ => "miasma-rotting"};
            args.PushMarkup(Loc.GetString(description));
        }

        private void OnRejuvenate(EntityUid uid, RottingComponent component, RejuvenateEvent args)
        {
            EntityManager.RemoveComponentDeferred<RottingComponent>(uid);
        }

        /// Containers

        private void OnEntInserted(EntityUid uid, AntiRottingContainerComponent component, EntInsertedIntoContainerMessage args)
        {
            if (TryComp<PerishableComponent>(args.Entity, out var perishable))
            {
                ModifyPreservationSource(args.Entity, true);
                ToggleDecomposition(args.Entity, false, perishable);
            }
        }

        private void OnEntRemoved(EntityUid uid, AntiRottingContainerComponent component, EntRemovedFromContainerMessage args)
        {
            // If we get de-parented due to entity shutdown don't add more flies.
            if (TryComp<PerishableComponent>(args.Entity, out var perishable) &&
                TryComp<MetaDataComponent>(uid, out var metadata) && metadata.EntityLifeStage < EntityLifeStage.Terminating)
            {
                ModifyPreservationSource(args.Entity, false);
                ToggleDecomposition(args.Entity, true, perishable);
            }
        }

        /// Fly stuff

        private void OnFliesInit(EntityUid uid, FliesComponent component, ComponentInit args)
        {
            component.VirtFlies = EntityManager.SpawnEntity("AmbientSoundSourceFlies", Transform(uid).Coordinates);
        }

        private void OnFliesShutdown(EntityUid uid, FliesComponent component, ComponentShutdown args)
        {
            if (!Terminating(uid) && !Deleted(uid))
                Del(component.VirtFlies);
        }

        /// Public functions

        public void ToggleDecomposition(EntityUid uid, bool decompose, PerishableComponent? perishable = null)
        {
            if (Terminating(uid) || !Resolve(uid, ref perishable, false))
                return;

            if (decompose == perishable.Progressing) // Saved a few cycles
                return;

            perishable.Progressing = decompose;

            if (!IsRotting(perishable))
                return;

            if (decompose)
            {
                EnsureComp<FliesComponent>(uid);
                return;
            }

            RemComp<FliesComponent>(uid);
        }

        /// <summary>
        /// Add or remove a preservation source.
        /// Remove is just "add = false"
        /// If we have 0 we remove the whole component.
        /// </summary>
        public void ModifyPreservationSource(EntityUid uid, bool add)
        {
            var component = EnsureComp<BodyPreservedComponent>(uid);

            if (add)
            {
                component.PreservationSources++;
                return;
            }

            component.PreservationSources--;

            if (component.PreservationSources == 0)
                RemCompDeferred(uid, component);
        }

        public string RequestPoolDisease()
        {
            // We reset the current time on this outbreak so people don't get unlucky at the transition time
            _diseaseTime = _timing.CurTime + _poolRepickTime;
            return _poolDisease;
        }
    }
}
