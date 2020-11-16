using Robust.Shared.Interfaces.GameObjects;

namespace Content.Shared.GameObjects.Components.Mobs.State
{
    /// <summary>
    ///     The standard state an entity is in; no negative effects.
    /// </summary>
    public abstract class SharedNormalState : IMobState
    {
        public abstract void EnterState(IEntity entity);

        public abstract void ExitState(IEntity entity);

        public abstract void UpdateState(IEntity entity);

        public bool CanInteract()
        {
            return true;
        }

        public bool CanMove()
        {
            return true;
        }

        public bool CanUse()
        {
            return true;
        }

        public bool CanThrow()
        {
            return true;
        }

        public bool CanSpeak()
        {
            return true;
        }

        public bool CanDrop()
        {
            return true;
        }

        public bool CanPickup()
        {
            return true;
        }

        public bool CanEmote()
        {
            return true;
        }

        public bool CanAttack()
        {
            return true;
        }

        public bool CanEquip()
        {
            return true;
        }

        public bool CanUnequip()
        {
            return true;
        }

        public bool CanChangeDirection()
        {
            return true;
        }
    }
}
