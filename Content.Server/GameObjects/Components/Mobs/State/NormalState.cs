using Content.Server.GameObjects.Components.Body;
using Content.Server.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Mobs.State;
using Robust.Server.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.Mobs.State
{
    public class NormalState : SharedNormalState
    {
        public override void EnterState(IEntity entity)
        {
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
                    if (ruinable.DeadThreshold == null)
                    {
                        break;
                    }

                    var modifier = (int) (ruinable.TotalDamage / (ruinable.DeadThreshold / 7f));

                    status.ChangeStatusEffectIcon(StatusEffect.Health,
                        "/Textures/Interface/StatusEffects/Human/human" + modifier + ".png");

                    break;
                }
                case BodyManagerComponent body:
                {
                    if (body.CriticalThreshold == null)
                    {
                        return;
                    }

                    var modifier = (int) (body.TotalDamage / (body.CriticalThreshold / 7f));

                    status.ChangeStatusEffectIcon(StatusEffect.Health,
                        "/Textures/Interface/StatusEffects/Human/human" + modifier + ".png");

                    break;
                }
                default:
                {
                    status.ChangeStatusEffectIcon(StatusEffect.Health,
                        "/Textures/Interface/StatusEffects/Human/human0.png");
                    break;
                }
            }
        }
    }
}
