using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Rotation;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Client.GameObjects.EntitySystems
{
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
            }

            if (playSound)
            {
                var file = AudioHelpers.GetRandomFileFromSoundCollection("bodyfall");
                Get<AudioSystem>().Play(file, entity, AudioHelpers.WithVariation(0.25f));
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
    }
}
