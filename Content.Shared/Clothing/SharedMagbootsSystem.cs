using Content.Shared.Movement.EntitySystems;
using Robust.Shared.GameObjects;

namespace Content.Shared.Clothing
{
    public class SharedMagbootsSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedMagbootsComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
        }

        private void OnRefreshMovespeed(EntityUid uid, SharedMagbootsComponent component, RefreshMovementSpeedModifiersEvent args)
        {
            args.ModifySpeed(component.WalkSpeedModifier, component.SprintSpeedModifier);
        }
    }
}
