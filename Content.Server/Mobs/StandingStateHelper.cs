using Content.Server.Interfaces.GameObjects.Components.Items;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Rotation;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Mobs
{
    public static class StandingStateHelper
    {
        /// <summary>
        ///     Set's the mob standing state to down.
        /// </summary>
        /// <param name="entity">The mob in question</param>
        /// <param name="playSound">Whether to play a sound when falling down or not</param>
        /// <param name="dropItems">Whether to make the mob drop all the items on his hands</param>
        /// <param name="force">Whether or not to check if the entity can fall.</param>
        /// <returns>False if the mob was already downed or couldn't set the state</returns>
        public static bool Down(IEntity entity, bool playSound = true, bool dropItems = true, bool force = false)
        {
            if (dropItems)
            {
                DropAllItemsInHands(entity, false);
            }

            if (!force && !EffectBlockerSystem.CanFall(entity))
            {
                return false;
            }

            if (!entity.TryGetComponent(out AppearanceComponent appearance))
            {
                return false;
            }

            var newState = RotationState.Horizontal;
            appearance.TryGetData<RotationState>(RotationVisuals.RotationState, out var oldState);

            if (newState != oldState)
            {
                appearance.SetData(RotationVisuals.RotationState, newState);
            }

            if (playSound)
            {
                IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<AudioSystem>()
                    .PlayFromEntity(AudioHelpers.GetRandomFileFromSoundCollection("bodyfall"), entity, AudioHelpers.WithVariation(0.25f));
            }

            return true;
        }

        /// <summary>
        ///     Sets the mob's standing state to standing.
        /// </summary>
        /// <param name="entity">The mob in question.</param>
        /// <returns>False if the mob was already standing or couldn't set the state</returns>
        public static bool Standing(IEntity entity)
        {
            if (!entity.TryGetComponent(out AppearanceComponent appearance)) return false;
            appearance.TryGetData<RotationState>(RotationVisuals.RotationState, out var oldState);
            var newState = RotationState.Vertical;
            if (newState == oldState)
                return false;

            appearance.SetData(RotationVisuals.RotationState, newState);

            return true;
        }

        public static void DropAllItemsInHands(IEntity entity, bool doMobChecks = true)
        {
            if (!entity.TryGetComponent(out IHandsComponent hands)) return;

            foreach (var heldItem in hands.GetAllHeldItems())
            {
                hands.Drop(heldItem.Owner, doMobChecks);
            }
        }
    }
}
