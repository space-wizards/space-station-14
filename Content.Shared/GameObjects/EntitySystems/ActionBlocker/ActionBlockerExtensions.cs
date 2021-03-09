#nullable enable
using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.EntitySystems.ActionBlocker
{
    public static class ActionBlockerExtensions
    {
        public static bool CanMove(this IEntity entity)
        {
            return ActionBlockerSystem.CanMove(entity);
        }

        public static bool CanInteract(this IEntity entity)
        {
            return ActionBlockerSystem.CanInteract(entity);
        }

        public static bool CanUse(this IEntity entity)
        {
            return ActionBlockerSystem.CanUse(entity);
        }

        public static bool CanThrow(this IEntity entity)
        {
            return ActionBlockerSystem.CanThrow(entity);
        }

        public static bool CanSpeak(this IEntity entity)
        {
            return ActionBlockerSystem.CanSpeak(entity);
        }

        public static bool CanDrop(this IEntity entity)
        {
            return ActionBlockerSystem.CanDrop(entity);
        }

        public static bool CanPickup(this IEntity entity)
        {
            return ActionBlockerSystem.CanPickup(entity);
        }

        public static bool CanEmote(this IEntity entity)
        {
            return ActionBlockerSystem.CanEmote(entity);
        }

        public static bool CanAttack(this IEntity entity)
        {
            return ActionBlockerSystem.CanAttack(entity);
        }

        public static bool CanEquip(this IEntity entity)
        {
            return ActionBlockerSystem.CanEquip(entity);
        }

        public static bool CanUnequip(this IEntity entity)
        {
            return ActionBlockerSystem.CanUnequip(entity);
        }

        public static bool CanChangeDirection(this IEntity entity)
        {
            return ActionBlockerSystem.CanChangeDirection(entity);
        }

        public static bool CanShiver(this IEntity entity)
        {
            return ActionBlockerSystem.CanShiver(entity);
        }

        public static bool CanSweat(this IEntity entity)
        {
            return ActionBlockerSystem.CanSweat(entity);
        }
    }
}
