using Content.Server.AI.Operators;
using Content.Server.Nutrition.Components;
using Content.Server.Nyanotrasen.Nutrition.Components;
using Content.Server.Weapon.Melee.Components;
using Content.Shared.Damage;
using Content.Shared.Nutrition.Components;

namespace Content.Server.Nyanotrasen.AI.Operators.Nutrition
{
    public sealed class EatFoliageOperator : AiOperator
    {
        private readonly EntityUid _owner;
        private readonly EntityUid _target;
        private float _interactionCooldown;

        public EatFoliageOperator(EntityUid owner, EntityUid target)
        {
            _owner = owner;
            _target = target;
        }

        public override Outcome Execute(float frameTime)
        {
            if (_interactionCooldown >= 0)
            {
                _interactionCooldown -= frameTime;
                return Outcome.Continuing;
            }

            var entities = IoCManager.Resolve<IEntityManager>();
            var damageableSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<DamageableSystem>();

            if (entities.Deleted(_target))
            {
                return Outcome.Success;
            }

            if (entities.HasComponent<FoliageComponent>(_target) &&
                entities.TryGetComponent<DamageableComponent>(_target, out var damageComponent) &&
                entities.TryGetComponent<HungerComponent>(_owner, out var hungerComponent) &&
                entities.TryGetComponent<MeleeWeaponComponent>(_owner, out var weaponComponent))
            {
                var damageDealt = damageableSystem.TryChangeDamage(_target, weaponComponent.Damage, damageable: damageComponent);
                if (damageDealt != null && damageDealt.Total > 0)
                {
                    hungerComponent.UpdateFood(damageDealt.Total.Float());
                }

                if (hungerComponent.CurrentHunger >= hungerComponent.HungerThresholds[HungerThreshold.Overfed] ||
                    entities.Deleted(_target))
                {
                    return Outcome.Success;
                }
            }

            return Outcome.Continuing;
        }
    }
}
