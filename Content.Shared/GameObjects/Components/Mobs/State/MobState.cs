using Robust.Shared.Interfaces.GameObjects;

namespace Content.Shared.GameObjects.Components.Mobs.State
{
    public abstract class MobState : IMobState
    {
        public virtual void EnterState(IEntity entity) { }

        public virtual void ExitState(IEntity entity) { }

        public virtual void UpdateState(IEntity entity) { }

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
