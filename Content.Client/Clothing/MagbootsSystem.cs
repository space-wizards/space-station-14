using Content.Shared.Clothing;
using Content.Shared.Movement.EntitySystems;
using Robust.Shared.GameObjects;

namespace Content.Client.Clothing
{
    public sealed class MagbootsSystem : EntitySystem
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
