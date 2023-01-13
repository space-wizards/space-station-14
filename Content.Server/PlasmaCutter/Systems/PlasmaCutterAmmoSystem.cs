using Content.Server.PlasmaCutter.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Server.Popups;

namespace Content.Server.PlasmaCutter.Systems
{
    public sealed class PlasmaCutterAmmoSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popup = default!;

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

            if (PlasmaCutterComponent.MaxFuel - PlasmaCutterComponent.CurrentFuel < component.RefillAmmo)
            {
                _popup.PopupEntity(Loc.GetString("pc-ammo-component-after-interact-full-text"), PlasmaCutterComponent.Owner, args.User);
                args.Handled = true;
                return;
            }

            PlasmaCutterComponent.CurrentFuel = Math.Min(PlasmaCutterComponent.MaxFuel, PlasmaCutterComponent.CurrentFuel + component.RefillAmmo);
            _popup.PopupEntity(Loc.GetString("pc-ammo-component-after-interact-refilled-text"), PlasmaCutterComponent.Owner, args.User);
            EntityManager.QueueDeleteEntity(uid);

            args.Handled = true;
        }
    }
}
