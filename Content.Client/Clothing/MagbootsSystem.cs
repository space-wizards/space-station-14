using Content.Shared.Clothing;
using Content.Shared.Movement.EntitySystems;

namespace Content.Client.Clothing
{
    public sealed class MagbootsSystem : SharedMagbootsSystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MagbootsComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
        }

        private void OnRefreshMovespeed(EntityUid uid, MagbootsComponent component, RefreshMovementSpeedModifiersEvent args)
        {
            args.ModifySpeed(component.WalkSpeedModifier, component.SprintSpeedModifier);
        }
    }
}
