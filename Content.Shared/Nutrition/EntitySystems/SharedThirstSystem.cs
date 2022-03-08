using Content.Shared.Movement.EntitySystems;
using Content.Shared.Nutrition.Components;
using Robust.Shared.GameObjects;

namespace Content.Shared.Nutrition.EntitySystems
{
    public sealed class SharedThirstSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedThirstComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
        }

        private void OnRefreshMovespeed(EntityUid uid, SharedThirstComponent component, RefreshMovementSpeedModifiersEvent args)
        {
            float mod = component.CurrentThirstThreshold == ThirstThreshold.Parched ? 0.75f : 1.0f;
            args.ModifySpeed(mod, mod);
        }
    }
}
