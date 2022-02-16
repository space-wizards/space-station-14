using Content.Server.Xray.Components;
using Content.Shared.Inventory.Events;
using Robust.Server.GameObjects;

namespace Content.Server.XRay
{
    public sealed class XRaySystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<XRayGlassesComponent, GotEquippedEvent>(OnEquipped);
            SubscribeLocalEvent<XRayGlassesComponent, GotUnequippedEvent>(OnUnequipped);
        }

        private void OnEquipped(EntityUid uid, XRayGlassesComponent component, GotEquippedEvent args)
        {
            if (args.SlotFlags != component.ActivationSlot)
                return;

            TryComp<EyeComponent>(args.Equipee, out var eyeComponent);
            if (eyeComponent == null) return;
            eyeComponent.DrawFov = false;
        }

        private void OnUnequipped(EntityUid uid, XRayGlassesComponent component, GotUnequippedEvent args)
        {
            TryComp<EyeComponent>(args.Equipee, out var eyeComponent);
            if (eyeComponent == null) return;
            eyeComponent.DrawFov = true;
        }
    }
}
