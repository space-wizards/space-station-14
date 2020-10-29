using Content.Server.GameObjects.Components.Damage;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Mobs.State;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.Mobs.State
{
    public class NormalState : SharedNormalState
    {
        public override void EnterState(IEntity entity)
        {
            EntitySystem.Get<StandingStateSystem>().Standing(entity);

            if (entity.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(DamageStateVisuals.State, DamageState.Alive);
            }

            UpdateState(entity);
        }

        public override void ExitState(IEntity entity) { }

        public override void UpdateState(IEntity entity)
        {
            if (!entity.TryGetComponent(out ServerStatusEffectsComponent status))
            {
                return;
            }

            if (!entity.TryGetComponent(out IDamageableComponent damageable))
            {
                status.ChangeStatusEffectIcon(StatusEffect.Health,
                    "/Textures/Interface/StatusEffects/Human/human0.png");
                return;
            }

            // TODO
            switch (damageable)
            {
                case RuinableComponent ruinable:
                {
                    if (!ruinable.Thresholds.TryGetValue(DamageState.Dead, out var threshold))
                    {
                        return;
                    }

                    var modifier = (int) (ruinable.TotalDamage / (threshold / 7f));

                    status.ChangeStatusEffectIcon(StatusEffect.Health,
                        "/Textures/Interface/StatusEffects/Human/human" + modifier + ".png");

                    break;
                }
                default:
                {
                    if (!damageable.Thresholds.TryGetValue(DamageState.Critical, out var threshold))
                    {
                        return;
                    }

                    var modifier = (int) (damageable.TotalDamage / (threshold / 7f));

                    status.ChangeStatusEffectIcon(StatusEffect.Health,
                        "/Textures/Interface/StatusEffects/Human/human" + modifier + ".png");
                    break;
                }
            }
        }
    }
}
