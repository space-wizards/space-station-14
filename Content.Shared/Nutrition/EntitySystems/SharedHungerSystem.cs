using Content.Shared.Movement.EntitySystems;
using Content.Shared.Nutrition.Components;
using Robust.Shared.GameObjects;

namespace Content.Shared.Nutrition.EntitySystems
{
    public class SharedHungerSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedHungerComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
        }

        private void OnRefreshMovespeed(EntityUid uid, SharedHungerComponent component, RefreshMovementSpeedModifiersEvent args)
        {
            float mod = component.CurrentHungerThreshold == HungerThreshold.Starving ? 0.75f : 1.0f;
            args.ModifySpeed(mod, mod);
        }
    }
}
