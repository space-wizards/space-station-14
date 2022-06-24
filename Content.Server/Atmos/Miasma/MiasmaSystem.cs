using Content.Shared.MobState;
using Content.Shared.Damage;
using Content.Shared.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Temperature.Systems;
using Content.Server.Body.Components;
using Content.Shared.Examine;
using Robust.Shared.Containers;

namespace Content.Server.Atmos.Miasma
{
    public sealed class MiasmaSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        /// Feel free to weak this if there are perf concerns
        private float UpdateRate = 5f;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var (rotting, perishable) in EntityQuery<RottingComponent, PerishableComponent>())
            {
                if (!perishable.Progressing)
                    continue;

                perishable.DeathAccumulator += frameTime;
                if (perishable.DeathAccumulator < perishable.RotAfter.TotalSeconds)
                    continue;

                perishable.RotAccumulator += frameTime;
                if (perishable.RotAccumulator < UpdateRate) // This is where it starts to get noticable on larger animals, no need to run every second
                    continue;

                perishable.RotAccumulator -= UpdateRate;

                EnsureComp<FliesComponent>(perishable.Owner);

                DamageSpecifier damage = new();
                damage.DamageDict.Add("Blunt", 0.3); // Slowly accumulate enough to gib after like half an hour
                damage.DamageDict.Add("Cellular", 0.3); // Cloning rework might use this eventually

                _damageableSystem.TryChangeDamage(perishable.Owner, damage, true, true);

                if (!TryComp<PhysicsComponent>(perishable.Owner, out var physics))
                    continue;
                // We need a way to get the mass of the mob alone without armor etc in the future

                float molRate = perishable.MolsPerSecondPerUnitMass * UpdateRate;

                var tileMix = _atmosphereSystem.GetTileMixture(Transform(perishable.Owner).Coordinates);
                if (tileMix != null)
                    tileMix.AdjustMoles(Gas.Miasma, molRate * physics.FixturesMass);
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
            // Containers
            SubscribeLocalEvent<AntiRottingContainerComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
            SubscribeLocalEvent<AntiRottingContainerComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
            // Fly audiovisual stuff
            SubscribeLocalEvent<FliesComponent, ComponentInit>(OnFliesInit);
            SubscribeLocalEvent<FliesComponent, ComponentShutdown>(OnFliesShutdown);
        }

        private void OnShutdown(EntityUid uid, RottingComponent component, ComponentShutdown args)
        {
            RemComp<FliesComponent>(uid);
            if (TryComp<PerishableComponent>(uid, out var perishable))
            {
                perishable.DeathAccumulator = 0;
                perishable.RotAccumulator = 0;
            }
        }

        private void OnTempChange(EntityUid uid, RottingComponent component, OnTemperatureChangeEvent args)
        {
            bool decompose = (args.CurrentTemperature > 274f);
            ToggleDecomposition(uid, decompose);
        }

        private void OnMobStateChanged(EntityUid uid, PerishableComponent component, MobStateChangedEvent args)
        {
            if (args.Component.IsDead())
                EnsureComp<RottingComponent>(uid);
        }

        private void OnGibbed(EntityUid uid, PerishableComponent component, BeingGibbedEvent args)
        {
            if (!TryComp<PhysicsComponent>(uid, out var physics))
                return;

            if (!component.Rotting)
                return;

            var molsToDump = (component.MolsPerSecondPerUnitMass * physics.FixturesMass) * component.DeathAccumulator;
            var tileMix = _atmosphereSystem.GetTileMixture(Transform(uid).Coordinates);
            if (tileMix != null)
                tileMix.AdjustMoles(Gas.Miasma, molsToDump);

            // Waste of entities to let these through
            foreach (var part in args.GibbedParts)
                EntityManager.DeleteEntity(part);
        }

        private void OnExamined(EntityUid uid, PerishableComponent component, ExaminedEvent args)
        {
            if (component.Rotting)
                args.PushMarkup(Loc.GetString("miasma-rotting"));
        }

        /// Containers

        private void OnEntInserted(EntityUid uid, AntiRottingContainerComponent component, EntInsertedIntoContainerMessage args)
        {
            if (TryComp<PerishableComponent>(args.Entity, out var perishable))
                ToggleDecomposition(args.Entity, false, perishable);
        }
        private void OnEntRemoved(EntityUid uid, AntiRottingContainerComponent component, EntRemovedFromContainerMessage args)
        {
            if (TryComp<PerishableComponent>(args.Entity, out var perishable))
                ToggleDecomposition(args.Entity, true, perishable);
        }


        /// Fly stuff

        private void OnFliesInit(EntityUid uid, FliesComponent component, ComponentInit args)
        {
            component.VirtFlies = EntityManager.SpawnEntity("AmbientSoundSourceFlies", Transform(uid).Coordinates);
            Transform(component.VirtFlies).AttachParent(uid);
        }

        private void OnFliesShutdown(EntityUid uid, FliesComponent component, ComponentShutdown args)
        {
            EntityManager.DeleteEntity(component.VirtFlies);
        }

        /// Public functions

        public void ToggleDecomposition(EntityUid uid, bool decompose, PerishableComponent? perishable = null)
        {
            if (!Resolve(uid, ref perishable))
                return;

            if (decompose == perishable.Progressing) // Saved a few cycles
                return;

            if (!HasComp<RottingComponent>(uid))
                return;

            if (!perishable.Rotting)
                return;

            if (decompose)
            {
                perishable.Progressing = true;
                EnsureComp<FliesComponent>(uid);
                return;
            }

            perishable.Progressing = false;
            RemComp<FliesComponent>(uid);
        }
    }
}
