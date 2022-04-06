using Content.Server.Disease.Components;
using Content.Server.Weapon.Melee;
using System.Linq;
using Robust.Shared.Random;
using Content.Shared.Movement.EntitySystems;
using Content.Server.Speech.Components;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.Damage;
using Content.Shared.MobState.Components;

namespace Content.Server.Disease
{
    /// <summary>
    /// Handles zombie propagation and inherent zombie traits
    /// </summary>
    public sealed class DiseaseZombieSystem : EntitySystem
    {
        [Dependency] private readonly DiseaseSystem _disease = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DiseaseZombieComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<DiseaseZombieComponent, MeleeHitEvent>(OnMeleeHit);
            SubscribeLocalEvent<DiseaseZombieComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeedModifiers);
        }
        /// <remarks>
        /// I would imagine that if this component got assigned to something other than a mob, it would throw hella errors.
        /// </remarks>
        private void OnComponentInit(EntityUid uid, DiseaseZombieComponent component, ComponentInit args)
        {
            if (!component.ApplyZombieTraits)
                return;

            _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
            EntityManager.EnsureComponent<DamageableComponent>(uid, out var damageable);
            _damageableSystem.SetAllDamage(damageable, 0);

            EntityManager.EnsureComponent<ReplacementAccentComponent>(uid).Accent = "zombie";

            SetupGhostRole(uid);
        }

        private void SetupGhostRole(EntityUid uid)
        {
            EntityManager.TryGetComponent<MetaDataComponent>(uid, out var metacomp);
            metacomp.EntityName = "zombified " + metacomp.EntityName;

            EntityManager.EnsureComponent<GhostTakeoverAvailableComponent>(uid, out var ghostcomp);
            ghostcomp.RoleName = metacomp.EntityName;
            ghostcomp.RoleDescription = "A malevolent creature of the dead."; //TODO: loc strings?.
            ghostcomp.RoleRules = "An evil zombie.";
        }

        ///<summary>
        ///This handles zombie disease transfer when a entity is hit.
        ///</summary>
        private void OnMeleeHit(EntityUid uid, DiseaseZombieComponent component, MeleeHitEvent args)
        {
            if (!args.HitEntities.Any())
                return;

            foreach (EntityUid entity in args.HitEntities)
            {
                if (uid != entity)
                    continue;

                if (HasComp<DiseaseCarrierComponent>(entity)) //can only infect disease carrying entities. TODO: make it work for all mobs.
                {
                    if (_robustRandom.Prob(component.Probability))
                    {
                        _disease.TryAddDisease(entity, "ZombieInfection");

                        if (!EntityManager.TryGetComponent<MobStateComponent>(entity, out var mobState)) //this is fine
                            return;

                        if (mobState.IsDead() || mobState.IsCritical()) //dead entities are eautomatically infected
                        {
                            EntityManager.EnsureComponent<DiseaseZombieComponent>(entity);
                        }
                        else if (mobState.IsAlive()) //heals when zombies bite live entities
                        {
                            //_damageableSystem.TryChangeDamage(uid, , true, false);
                        }
                    }
                }
            }
        }

        private void OnRefreshMovementSpeedModifiers(EntityUid uid, DiseaseZombieComponent component, RefreshMovementSpeedModifiersEvent args)
        {
            args.ModifySpeed(component.SlowAmount, component.SlowAmount);
        }
    }
}
