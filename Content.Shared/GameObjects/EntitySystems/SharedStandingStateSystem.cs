#nullable enable
using Content.Shared.GameObjects.EntitySystems.EffectBlocker;
using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.EntitySystems
{
    public abstract class SharedStandingStateSystem : EntitySystem
    {
        protected abstract bool OnDown(IEntity entity, bool playSound = true, bool dropItems = true,
            bool force = false);

        protected abstract bool OnStand(IEntity entity);

        /// <summary>
        ///     Set's the mob standing state to down.
        /// </summary>
        /// <param name="entity">The mob in question</param>
        /// <param name="playSound">Whether to play a sound when falling down or not</param>
        /// <param name="dropItems">Whether to make the mob drop all the items on his hands</param>
        /// <param name="force">Whether or not to check if the entity can fall.</param>
        /// <returns>False if the mob was already downed or couldn't set the state</returns>
        public bool Down(IEntity entity, bool playSound = true, bool dropItems = true, bool force = false)
        {
            if (dropItems)
            {
                DropAllItemsInHands(entity, false);
            }

            if (!force && !EffectBlockerSystem.CanFall(entity))
            {
                return false;
            }

            return OnDown(entity, playSound, dropItems, force);
        }

        /// <summary>
        ///     Sets the mob's standing state to standing.
        /// </summary>
        /// <param name="entity">The mob in question.</param>
        /// <returns>False if the mob was already standing or couldn't set the state</returns>
        public bool Standing(IEntity entity)
        {
            return OnStand(entity);
        }

        public virtual void DropAllItemsInHands(IEntity entity, bool doMobChecks = true)
        {
        }
    }
}
