using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Mobs.State
{
    public abstract class BaseMobState : IMobState
    {
        protected abstract DamageState DamageState { get; }

        public virtual bool IsAlive()
        {
            return DamageState == DamageState.Alive;
        }

        public virtual bool IsCritical()
        {
            return DamageState == DamageState.Critical;
        }

        public virtual bool IsDead()
        {
            return DamageState == DamageState.Dead;
        }

        public virtual bool IsIncapacitated()
        {
            return IsCritical() || IsDead();
        }

        public virtual void EnterState(IEntity entity) { }

        public virtual void ExitState(IEntity entity) { }

        public virtual void UpdateState(IEntity entity, int threshold) { }

        public virtual void ExposeData(ObjectSerializer serializer) { }

        public virtual bool CanInteract()
        {
            return true;
        }

        public virtual bool CanMove()
        {
            return true;
        }

        public virtual bool CanUse()
        {
            return true;
        }

        public virtual bool CanThrow()
        {
            return true;
        }

        public virtual bool CanSpeak()
        {
            return true;
        }

        public virtual bool CanDrop()
        {
            return true;
        }

        public virtual bool CanPickup()
        {
            return true;
        }

        public virtual bool CanEmote()
        {
            return true;
        }

        public virtual bool CanAttack()
        {
            return true;
        }

        public virtual bool CanEquip()
        {
            return true;
        }

        public virtual bool CanUnequip()
        {
            return true;
        }

        public virtual bool CanChangeDirection()
        {
            return true;
        }
    }
}
