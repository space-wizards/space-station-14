using Content.Server.PlasmaCutter.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;

namespace Content.Server.PlasmaCutter.Systems
{
    public sealed class PlasmaCutterAmmoSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PlasmaCutterAmmoComponent, ExaminedEvent>(OnExamine);
            SubscribeLocalEvent<PlasmaCutterAmmoComponent, AfterInteractEvent>(OnAfterInteract);
        }

        private void OnExamine(EntityUid uid, PlasmaCutterAmmoComponent component, ExaminedEvent args)
        {
            var examineMessage = Loc.GetString("pc-ammo-component-on-examine-text", ("ammo", component.RefillAmmo));
            args.PushText(examineMessage);
        }

        private void OnAfterInteract(EntityUid uid, PlasmaCutterAmmoComponent component, AfterInteractEvent args)
        {
            if (args.Handled || !args.CanReach)
                return;

            if (args.Target is not { Valid: true } target ||
                !EntityManager.TryGetComponent(target, out PlasmaCutterComponent? PlasmaCutterComponent))
                return;

            if (PlasmaCutterComponent.MaxAmmo - PlasmaCutterComponent.CurrentAmmo < component.RefillAmmo)
            {
                PlasmaCutterComponent.Owner.PopupMessage(args.User, Loc.GetString("pc-ammo-component-after-interact-full-text"));
                args.Handled = true;
                return;
            }

            PlasmaCutterComponent.CurrentAmmo = Math.Min(PlasmaCutterComponent.MaxAmmo, PlasmaCutterComponent.CurrentAmmo + component.RefillAmmo);
            PlasmaCutterComponent.Owner.PopupMessage(args.User, Loc.GetString("pc-ammo-component-after-interact-refilled-text"));
            EntityManager.QueueDeleteEntity(uid);

            args.Handled = true;
        }
    }
}
