using Content.Shared.Alert;
using Content.Shared.Hands;
using Content.Shared.Standing;
using Robust.Shared.GameObjects;

namespace Content.Shared.MobState.State
{
    /// <summary>
    ///     A state in which an entity is disabled from acting due to sufficient damage (considered unconscious).
    /// </summary>
    public abstract class SharedCriticalMobState : BaseMobState
    {
        protected override DamageState DamageState => DamageState.Critical;

        public override void EnterState(IEntity entity)
        {
            base.EnterState(entity);

            if (entity.TryGetComponent(out SharedAlertsComponent? status))
            {
                status.ShowAlert(AlertType.HumanCrit); // TODO: combine humancrit-0 and humancrit-1 into a gif and display it
            }

            EntitySystem.Get<StandingStateSystem>().Down(entity);

            if (entity.TryGetComponent(out SharedAppearanceComponent? appearance))
            {
                appearance.SetData(DamageStateVisuals.State, DamageState.Critical);
            }
        }

        public override void ExitState(IEntity entity)
        {
            base.ExitState(entity);

            EntitySystem.Get<StandingStateSystem>().Stand(entity);
        }

        public override bool CanInteract()
        {
            return false;
        }

        public override bool CanMove()
        {
            return false;
        }

        public override bool CanUse()
        {
            return false;
        }

        public override bool CanThrow()
        {
            return false;
        }

        public override bool CanSpeak()
        {
            return false;
        }

        public override bool CanDrop()
        {
            return false;
        }

        public override bool CanPickup()
        {
            return false;
        }

        public override bool CanEmote()
        {
            return false;
        }

        public override bool CanAttack()
        {
            return false;
        }

        public override bool CanEquip()
        {
            return false;
        }

        public override bool CanUnequip()
        {
            return false;
        }

        public override bool CanChangeDirection()
        {
            return false;
        }
    }
}
