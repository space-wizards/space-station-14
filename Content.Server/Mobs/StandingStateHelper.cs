using Content.Server.Interfaces.GameObjects;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

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
        /// <returns>False if the mob was already downed or couldn't set the state</returns>
        public static bool Down(IEntity entity, bool playSound = true, bool dropItems = true)
        {
            if (!entity.TryGetComponent(out AppearanceComponent appearance)) return false;

            appearance.TryGetData<SharedSpeciesComponent.MobState>(SharedSpeciesComponent.MobVisuals.RotationState, out var oldState);

                var newState = SharedSpeciesComponent.MobState.Down;
            if (newState == oldState)
                return false;

            appearance.SetData(SharedSpeciesComponent.MobVisuals.RotationState, newState);

            if (playSound)
                IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<AudioSystem>()
                    .Play(AudioHelpers.GetRandomFileFromSoundCollection("bodyfall"), entity, AudioHelpers.WithVariation(0.25f));

            if(dropItems)
                DropAllItemsInHands(entity);

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
            appearance.TryGetData<SharedSpeciesComponent.MobState>(SharedSpeciesComponent.MobVisuals.RotationState, out var oldState);
            var newState = SharedSpeciesComponent.MobState.Standing;
            if (newState == oldState)
                return false;

            appearance.SetData(SharedSpeciesComponent.MobVisuals.RotationState, newState);

            return true;
        }

        public static void DropAllItemsInHands(IEntity entity)
        {
            if (!entity.TryGetComponent(out IHandsComponent hands)) return;

            foreach (var heldItem in hands.GetAllHeldItems())
            {
                hands.Drop(heldItem.Owner);
            }
        }
    }
}
