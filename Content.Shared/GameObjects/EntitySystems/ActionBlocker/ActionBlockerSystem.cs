using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Shared.GameObjects.EntitySystems.ActionBlocker
{
    /// <summary>
    /// Utility methods to check if a specific entity is allowed to perform an action.
    /// For effects see <see cref="EffectBlockerSystem"/>
    /// </summary>
    [UsedImplicitly]
    public class ActionBlockerSystem : EntitySystem
    {
        public static bool CanMove(IEntity entity)
        {
            var canMove = true;

            foreach (var blockers in entity.GetAllComponents<IActionBlocker>())
            {
                canMove &= blockers.CanMove(); // Sets var to false if false
            }

            return canMove;
        }

        public static bool CanInteract(IEntity entity)
        {
            var canInteract = true;

            foreach (var blockers in entity.GetAllComponents<IActionBlocker>())
            {
                canInteract &= blockers.CanInteract();
            }

            return canInteract;
        }

        public static bool CanUse(IEntity entity)
        {
            var canUse = true;

            foreach (var blockers in entity.GetAllComponents<IActionBlocker>())
            {
                canUse &= blockers.CanUse();
            }

            return canUse;
        }

        public static bool CanThrow(IEntity entity)
        {
            var canThrow = true;

            foreach (var blockers in entity.GetAllComponents<IActionBlocker>())
            {
                canThrow &= blockers.CanThrow();
            }

            return canThrow;
        }

        public static bool CanSpeak(IEntity entity)
        {
            var canSpeak = true;

            foreach (var blockers in entity.GetAllComponents<IActionBlocker>())
            {
                canSpeak &= blockers.CanSpeak();
            }

            return canSpeak;
        }

        public static bool CanDrop(IEntity entity)
        {
            var canDrop = true;

            foreach (var blockers in entity.GetAllComponents<IActionBlocker>())
            {
                canDrop &= blockers.CanDrop();
            }

            return canDrop;
        }

        public static bool CanPickup(IEntity entity)
        {
            var canPickup = true;

            foreach (var blockers in entity.GetAllComponents<IActionBlocker>())
            {
                canPickup &= blockers.CanPickup();
            }

            return canPickup;
        }

        public static bool CanEmote(IEntity entity)
        {
            var canEmote = true;

            foreach (var blockers in entity.GetAllComponents<IActionBlocker>())
            {
                canEmote &= blockers.CanEmote();
            }

            return canEmote;
        }

        public static bool CanAttack(IEntity entity)
        {
            var canAttack = true;

            foreach (var blockers in entity.GetAllComponents<IActionBlocker>())
            {
                canAttack &= blockers.CanAttack();
            }

            return canAttack;
        }

        public static bool CanEquip(IEntity entity)
        {
            var canEquip = true;

            foreach (var blockers in entity.GetAllComponents<IActionBlocker>())
            {
                canEquip &= blockers.CanEquip();
            }

            return canEquip;
        }

        public static bool CanUnequip(IEntity entity)
        {
            var canUnequip = true;

            foreach (var blockers in entity.GetAllComponents<IActionBlocker>())
            {
                canUnequip &= blockers.CanUnequip();
            }

            return canUnequip;
        }

        public static bool CanChangeDirection(IEntity entity)
        {
            var canChangeDirection = true;

            foreach (var blockers in entity.GetAllComponents<IActionBlocker>())
            {
                canChangeDirection &= blockers.CanChangeDirection();
            }

            return canChangeDirection;
        }

        public static bool CanShiver(IEntity entity)
        {
            var canShiver = true;
            foreach (var component in entity.GetAllComponents<IActionBlocker>())
            {
                canShiver &= component.CanShiver();
            }
            return canShiver;
        }

        public static bool CanSweat(IEntity entity)
        {
            var canSweat = true;
            foreach (var component in entity.GetAllComponents<IActionBlocker>())
            {
                canSweat &= component.CanSweat();
            }
            return canSweat;
        }
    }
}
