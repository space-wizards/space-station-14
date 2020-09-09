using System.Linq;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Shared.GameObjects.EntitySystems
{
    /// <summary>
    /// This interface gives components the ability to block certain actions from
    /// being done by the owning entity. For effects see <see cref="IEffectBlocker"/>
    /// </summary>
    public interface IActionBlocker
    {
        bool CanMove() => true;
        bool CanInteract() => true;
        bool CanUse() => true;
        bool CanThrow() => true;
        bool CanSpeak() => true;
        bool CanDrop() => true;
        bool CanPickup() => true;
        bool CanEmote() => true;
        bool CanAttack() => true;

        bool CanEquip() => true;

        bool CanUnequip() => true;

        bool CanChangeDirection() => true;

        bool CanShiver() => true;
        bool CanSweat() => true;
    }

    /// <summary>
    /// Utility methods to check if a specific entity is allowed to perform an action.
    /// For effects see <see cref="EffectBlockerSystem"/>
    /// </summary>
    public class ActionBlockerSystem : EntitySystem
    {
        public static bool CanMove(IEntity entity)
        {
            bool canmove = true;
            foreach (var actionblockercomponents in entity.GetAllComponents<IActionBlocker>())
            {
                canmove &= actionblockercomponents.CanMove(); // Sets var to false if false
            }

            return canmove;
        }

        public static bool CanInteract(IEntity entity)
        {
            bool caninteract = true;
            foreach (var actionblockercomponents in entity.GetAllComponents<IActionBlocker>())
            {
                caninteract &= actionblockercomponents.CanInteract();
            }

            return caninteract;
        }

        public static bool CanUse(IEntity entity)
        {
            bool canuse = true;
            foreach (var actionblockercomponents in entity.GetAllComponents<IActionBlocker>())
            {
                canuse &= actionblockercomponents.CanUse();
            }

            return canuse;
        }

        public static bool CanThrow(IEntity entity)
        {
            bool canthrow = true;
            foreach (var actionblockercomponents in entity.GetAllComponents<IActionBlocker>())
            {
                canthrow &= actionblockercomponents.CanThrow();
            }

            return canthrow;
        }

        public static bool CanSpeak(IEntity entity)
        {
            bool canspeak = true;
            foreach (var actionblockercomponents in entity.GetAllComponents<IActionBlocker>())
            {
                canspeak &= actionblockercomponents.CanSpeak();
            }

            return canspeak;
        }

        public static bool CanDrop(IEntity entity)
        {
            bool candrop = true;
            foreach (var actionblockercomponents in entity.GetAllComponents<IActionBlocker>())
            {
                candrop &= actionblockercomponents.CanDrop();
            }

            return candrop;
        }

        public static bool CanPickup(IEntity entity)
        {
            bool canpickup = true;
            foreach (var actionblockercomponents in entity.GetAllComponents<IActionBlocker>())
            {
                canpickup &= actionblockercomponents.CanPickup();
            }

            return canpickup;
        }

        public static bool CanEmote(IEntity entity)
        {
            bool canemote = true;

            foreach (var actionblockercomponents in entity.GetAllComponents<IActionBlocker>())
            {
                canemote &= actionblockercomponents.CanEmote();
            }

            return canemote;
        }

        public static bool CanAttack(IEntity entity)
        {
            bool canattack = true;

            foreach (var actionblockercomponents in entity.GetAllComponents<IActionBlocker>())
            {
                canattack &= actionblockercomponents.CanAttack();
            }

            return canattack;
        }

        public static bool CanEquip(IEntity entity)
        {
            bool canequip = true;

            foreach (var actionblockercomponents in entity.GetAllComponents<IActionBlocker>())
            {
                canequip &= actionblockercomponents.CanEquip();
            }

            return canequip;
        }

        public static bool CanUnequip(IEntity entity)
        {
            bool canunequip = true;

            foreach (var actionblockercomponents in entity.GetAllComponents<IActionBlocker>())
            {
                canunequip &= actionblockercomponents.CanUnequip();
            }

            return canunequip;
        }

        public static bool CanChangeDirection(IEntity entity)
        {
            bool canchangedirection = true;

            foreach (var actionblockercomponents in entity.GetAllComponents<IActionBlocker>())
            {
                canchangedirection &= actionblockercomponents.CanChangeDirection();
            }

            return canchangedirection;
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
