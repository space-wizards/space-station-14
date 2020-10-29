using Content.Server.Interfaces.GameObjects.Components.Items;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Rotation;
using Content.Shared.GameObjects.EntitySystems;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class StandingStateSystem : SharedStandingStateSystem
    {
        protected override bool OnDown(IEntity entity, bool playSound = true, bool dropItems = true, bool force = false)
        {
            if (!entity.TryGetComponent(out AppearanceComponent appearance))
            {
                return false;
            }

            var newState = RotationState.Horizontal;
            appearance.TryGetData<RotationState>(RotationVisuals.RotationState, out var oldState);

            if (newState != oldState)
            {
                appearance.SetData(RotationVisuals.RotationState, newState);

                if (playSound)
                {
                    var file = AudioHelpers.GetRandomFileFromSoundCollection("bodyfall");
                    Get<AudioSystem>().PlayFromEntity(file, entity, AudioHelpers.WithVariation(0.25f));
                }
            }

            return true;
        }

        protected override bool OnStand(IEntity entity)
        {
            if (!entity.TryGetComponent(out AppearanceComponent appearance)) return false;

            appearance.TryGetData<RotationState>(RotationVisuals.RotationState, out var oldState);
            var newState = RotationState.Vertical;

            if (newState == oldState) return false;

            appearance.SetData(RotationVisuals.RotationState, newState);

            return true;
        }

        public override void DropAllItemsInHands(IEntity entity, bool doMobChecks = true)
        {
            base.DropAllItemsInHands(entity, doMobChecks);

            if (!entity.TryGetComponent(out IHandsComponent hands)) return;

            foreach (var heldItem in hands.GetAllHeldItems())
            {
                hands.Drop(heldItem.Owner, doMobChecks);
            }
        }

        //TODO: RotationState can be null and I want to burn all lifeforms in the universe for this!!!
        //If you use these it's atleast slightly less painful (null is treated as false)
        public bool IsStanding(IEntity entity)
        {
            return entity.TryGetComponent<AppearanceComponent>(out var appearance)
                && appearance.TryGetData<RotationState>(RotationVisuals.RotationState, out var rotation)
                && rotation == RotationState.Vertical;
        }

        public bool IsDown(IEntity entity)
        {
            return entity.TryGetComponent<AppearanceComponent>(out var appearance)
                && appearance.TryGetData<RotationState>(RotationVisuals.RotationState, out var rotation)
                && rotation == RotationState.Horizontal;
        }
    }
}
