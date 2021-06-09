using Content.Shared.Rotation;
using Content.Shared.Standing;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.Standing
{
    public class StandingStateSystem : SharedStandingStateSystem
    {
        protected override bool OnDown(IEntity entity, bool playSound = true, bool dropItems = true, bool force = false)
        {
            if (!entity.TryGetComponent(out AppearanceComponent? appearance))
            {
                return false;
            }

            var newState = RotationState.Horizontal;
            appearance.TryGetData<RotationState>(RotationVisuals.RotationState, out var oldState);

            if (newState != oldState)
            {
                appearance.SetData(RotationVisuals.RotationState, newState);
            }

            return true;
        }

        protected override bool OnStand(IEntity entity)
        {
            if (!entity.TryGetComponent(out AppearanceComponent? appearance)) return false;

            appearance.TryGetData<RotationState>(RotationVisuals.RotationState, out var oldState);
            var newState = RotationState.Vertical;

            if (newState == oldState) return false;

            appearance.SetData(RotationVisuals.RotationState, newState);

            return true;
        }
    }
}
