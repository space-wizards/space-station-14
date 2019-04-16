using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Server.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects
{
    /// <summary>
    /// Defines the blocking effect of each damage state, and what effects to apply upon entering or exiting the state
    /// </summary>
    public interface DamageState : IActionBlocker
    {
        void EnterState(IEntity entity);

        void ExitState(IEntity entity);
    }

    /// <summary>
    /// Standard state that a species is at with no damage or negative effect
    /// </summary>
    public struct NormalState : DamageState
    {
        public void EnterState(IEntity entity)
        {
        }

        public void ExitState(IEntity entity)
        {
        }

        bool IActionBlocker.CanInteract()
        {
            return true;
        }

        bool IActionBlocker.CanMove()
        {
            return true;
        }

        bool IActionBlocker.CanUse()
        {
            return true;
        }
    }

    /// <summary>
    /// A state in which you are disabled from acting due to damage
    /// </summary>
    public struct CriticalState : DamageState
    {
        public void EnterState(IEntity entity)
        {
        }

        public void ExitState(IEntity entity)
        {
        }

        bool IActionBlocker.CanInteract()
        {
            return false;
        }

        bool IActionBlocker.CanMove()
        {
            return false;
        }

        bool IActionBlocker.CanUse()
        {
            return false;
        }
    }

    /// <summary>
    /// A damage state which will allow ghosting out of mobs
    /// </summary>
    public struct DeadState : DamageState
    {
        public void EnterState(IEntity entity)
        {
            if (entity.TryGetComponent(out AppearanceComponent appearance))
            {
                var newState = SharedSpeciesComponent.MobState.Down;
                appearance.SetData(SharedSpeciesComponent.MobVisuals.RotationState, newState);
            }
        }

        public void ExitState(IEntity entity)
        {
            if (entity.TryGetComponent(out AppearanceComponent appearance))
            {
                var newState = SharedSpeciesComponent.MobState.Stand;
                appearance.SetData(SharedSpeciesComponent.MobVisuals.RotationState, newState);
            }
        }

        bool IActionBlocker.CanInteract()
        {
            return false;
        }

        bool IActionBlocker.CanMove()
        {
            return false;
        }

        bool IActionBlocker.CanUse()
        {
            return false;
        }
    }
}
