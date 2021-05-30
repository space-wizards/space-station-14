#nullable enable
using System.Diagnostics.CodeAnalysis;
using Content.Shared.GameObjects.Components.Mobs.Speech;
using Content.Shared.GameObjects.EntitySystems.EffectBlocker;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

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

            foreach (var blocker in entity.GetAllComponents<IActionBlocker>())
            {
                canMove &= blocker.CanMove(); // Sets var to false if false
            }

            return canMove;
        }

        public static bool CanInteract([NotNullWhen(true)] IEntity? entity)
        {
            if (entity == null)
            {
                return false;
            }

            var canInteract = true;

            foreach (var blocker in entity.GetAllComponents<IActionBlocker>())
            {
                canInteract &= blocker.CanInteract();
            }

            return canInteract;
        }

        public static bool CanUse([NotNullWhen(true)] IEntity? entity)
        {
            if (entity == null)
            {
                return false;
            }

            var canUse = true;

            foreach (var blocker in entity.GetAllComponents<IActionBlocker>())
            {
                canUse &= blocker.CanUse();
            }

            return canUse;
        }

        public static bool CanThrow(IEntity entity)
        {
            var canThrow = true;

            foreach (var blocker in entity.GetAllComponents<IActionBlocker>())
            {
                canThrow &= blocker.CanThrow();
            }

            return canThrow;
        }

        public static bool CanSpeak(IEntity entity)
        {
            if (!entity.HasComponent<SharedSpeechComponent>())
                return false;

            var canSpeak = true;

            foreach (var blocker in entity.GetAllComponents<IActionBlocker>())
            {
                canSpeak &= blocker.CanSpeak();
            }

            return canSpeak;
        }

        public static bool CanDrop(IEntity entity)
        {
            var canDrop = true;

            foreach (var blocker in entity.GetAllComponents<IActionBlocker>())
            {
                canDrop &= blocker.CanDrop();
            }

            return canDrop;
        }

        public static bool CanPickup(IEntity entity)
        {
            var canPickup = true;

            foreach (var blocker in entity.GetAllComponents<IActionBlocker>())
            {
                canPickup &= blocker.CanPickup();
            }

            return canPickup;
        }

        public static bool CanEmote(IEntity entity)
        {
            if (!entity.HasComponent<SharedEmotingComponent>())
                return false;

            var canEmote = true;

            foreach (var blocker in entity.GetAllComponents<IActionBlocker>())
            {
                canEmote &= blocker.CanEmote();
            }

            return canEmote;
        }

        public static bool CanAttack(IEntity entity)
        {
            var canAttack = true;

            foreach (var blocker in entity.GetAllComponents<IActionBlocker>())
            {
                canAttack &= blocker.CanAttack();
            }

            return canAttack;
        }

        public static bool CanEquip(IEntity entity)
        {
            var canEquip = true;

            foreach (var blocker in entity.GetAllComponents<IActionBlocker>())
            {
                canEquip &= blocker.CanEquip();
            }

            return canEquip;
        }

        public static bool CanUnequip(IEntity entity)
        {
            var canUnequip = true;

            foreach (var blocker in entity.GetAllComponents<IActionBlocker>())
            {
                canUnequip &= blocker.CanUnequip();
            }

            return canUnequip;
        }

        public static bool CanChangeDirection(IEntity entity)
        {
            var canChangeDirection = true;

            foreach (var blocker in entity.GetAllComponents<IActionBlocker>())
            {
                canChangeDirection &= blocker.CanChangeDirection();
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
