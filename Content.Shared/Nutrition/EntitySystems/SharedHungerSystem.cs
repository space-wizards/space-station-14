using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;

namespace Content.Shared.Nutrition.EntitySystems
{
    public sealed class SharedHungerSystem : EntitySystem
    {
        [Dependency] private readonly SharedJetpackSystem _jetpack = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedHungerComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
        }

        private void OnRefreshMovespeed(EntityUid uid, SharedHungerComponent component, RefreshMovementSpeedModifiersEvent args)
        {
            if (_jetpack.IsUserFlying(component.Owner))
                return;

            float mod = component.CurrentHungerThreshold <= HungerThreshold.Starving ? 0.75f : 1.0f;
            args.ModifySpeed(mod, mod);
        }
    }
}
