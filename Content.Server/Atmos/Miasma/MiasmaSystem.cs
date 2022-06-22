using Content.Shared.MobState;
using Content.Shared.Damage;
using Content.Shared.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Temperature.Components;
using Content.Server.Body.Components;
using Content.Shared.Examine;
using Content.Shared.Audio;
using Robust.Shared.Containers;

namespace Content.Server.Atmos.Miasma
{
    public sealed class MiasmaSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
        /// Feel free to weak this if there are perf concerns
        private float UpdateRate = 5f;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var (rotting, perishable) in EntityQuery<RottingComponent, PerishableComponent>())
            {
                if (!perishable.Progressing)
                    continue;

                if (TryComp<TemperatureComponent>(perishable.Owner, out var temp) && temp.CurrentTemperature < 274f)
                    continue;

                perishable.DeathAccumulator += frameTime;
                if (perishable.DeathAccumulator < perishable.RotAfter.TotalSeconds)
                    continue;

                perishable.RotAccumulator += frameTime;
                if (perishable.RotAccumulator < UpdateRate) // This is where it starts to get noticable on larger animals, no need to run every second
                    continue;

                perishable.RotAccumulator -= UpdateRate;

                EnsureComp<FliesComponent>(perishable.Owner);
                _ambientSound.SetAmbience(perishable.Owner, true);

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
            SubscribeLocalEvent<RottingComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<PerishableComponent, MobStateChangedEvent>(OnMobStateChanged);
            SubscribeLocalEvent<PerishableComponent, BeingGibbedEvent>(OnGibbed);
            SubscribeLocalEvent<PerishableComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<AntiRottingContainerComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
            SubscribeLocalEvent<AntiRottingContainerComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        }

        private void OnShutdown(EntityUid uid, RottingComponent component, ComponentShutdown args)
        {
            RemComp<FliesComponent>(uid);
            if (TryComp<PerishableComponent>(uid, out var perishable))
            {
                perishable.DeathAccumulator = 0;
                perishable.RotAccumulator = 0;
            }
            _ambientSound.SetAmbience(uid, false); // Ideally this will support dynamic sources in the future, I really didn't want to make flies an entity
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

            foreach (var part in args.GibbedParts)
            {
                EntityManager.DeleteEntity(part);
            }
        }

        private void OnExamined(EntityUid uid, PerishableComponent component, ExaminedEvent args)
        {
            if (component.Rotting)
                args.PushMarkup(Loc.GetString("miasma-rotting"));
        }

        private void OnEntInserted(EntityUid uid, AntiRottingContainerComponent component, EntInsertedIntoContainerMessage args)
        {
            if (TryComp<PerishableComponent>(args.Entity, out var perishable))
                perishable.Progressing = false;
        }

        private void OnEntRemoved(EntityUid uid, AntiRottingContainerComponent component, EntRemovedFromContainerMessage args)
        {
            if (TryComp<PerishableComponent>(args.Entity, out var perishable))
                perishable.Progressing = true;
        }
    }
}
