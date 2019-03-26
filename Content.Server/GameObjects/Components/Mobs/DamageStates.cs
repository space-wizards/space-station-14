using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Mobs;
using SS14.Server.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Maths;

namespace Content.Server.GameObjects
{
    /// <summary>
    /// Defines the blocking effect of each damage state, and what effects to apply upon entering or exiting the state
    /// </summary>
    public interface DamageState : IActionBlocker
    { 
        void EnterState(IEntity entity, AppearanceComponent appearance);

        void ExitState(IEntity entity, AppearanceComponent appearance);
    }

    /// <summary>
    /// Standard state that a species is at with no damage or negative effect
    /// </summary>
    public struct NormalState : DamageState
    {
        public void EnterState(IEntity entity, AppearanceComponent appearance) {}

        public void ExitState(IEntity entity, AppearanceComponent appearance) {}

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
        public void EnterState(IEntity entity, AppearanceComponent appearance) {
            if (!entity.TryGetComponent<PlayerInputMoverComponent>(out var mover))
            {
                return;
            }
            mover.Disabled = true;
        }

        public void ExitState(IEntity entity, AppearanceComponent appearance) {
            if (!entity.TryGetComponent<PlayerInputMoverComponent>(out var mover))
            {
                return;
            }
            mover.Disabled = false;
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
        public void EnterState(IEntity entity, AppearanceComponent appearance)
        {
            var newstate = SpeciesComponent.MobState.Down;
            appearance.SetData(SpeciesComponent.MobVisuals.RotationState, newstate);
            if (!entity.TryGetComponent<PlayerInputMoverComponent>(out var mover))
            {
                return;
            }
            mover.Disabled = true;
        }

        public void ExitState(IEntity entity, AppearanceComponent appearance)
        {
            var newstate = SpeciesComponent.MobState.Stand;
            appearance.SetData(SpeciesComponent.MobVisuals.RotationState, newstate);
            if (!entity.TryGetComponent<PlayerInputMoverComponent>(out var mover))
            {
                return;
            }
            mover.Disabled = false;
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
