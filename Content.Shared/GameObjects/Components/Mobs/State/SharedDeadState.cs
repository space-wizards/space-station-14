using Robust.Shared.Interfaces.GameObjects;

namespace Content.Shared.GameObjects.Components.Mobs.State
{
    public abstract class SharedDeadState : IMobState
    {
        public abstract void EnterState(IEntity entity);

        public abstract void ExitState(IEntity entity);

        public abstract void UpdateState(IEntity entity);

        public bool CanInteract()
        {
            return false;
        }

        public bool CanMove()
        {
            return false;
        }

        public bool CanUse()
        {
            return false;
        }

        public bool CanThrow()
        {
            return false;
        }

        public bool CanSpeak()
        {
            return false;
        }

        public bool CanDrop()
        {
            return false;
        }

        public bool CanPickup()
        {
            return false;
        }

        public bool CanEmote()
        {
            return false;
        }

        public bool CanAttack()
        {
            return false;
        }

        public bool CanEquip()
        {
            return false;
        }

        public bool CanUnequip()
        {
            return false;
        }

        public bool CanChangeDirection()
        {
            return false;
        }
    }
}
