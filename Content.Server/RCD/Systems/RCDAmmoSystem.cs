using Content.Server.RCD.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Server.RCD.Systems
{
    public sealed class RCDAmmoSystem : EntitySystem
    {
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RCDAmmoComponent, ExaminedEvent>(OnExamine);
            SubscribeLocalEvent<RCDAmmoComponent, AfterInteractEvent>(OnAfterInteract);
        }

        private void OnExamine(EntityUid uid, RCDAmmoComponent component, ExaminedEvent args)
        {
            var examineMessage = Loc.GetString("rcd-ammo-component-on-examine-text", ("ammo", component.RefillAmmo));
            args.PushText(examineMessage);
        }

        private void OnAfterInteract(EntityUid uid, RCDAmmoComponent component, AfterInteractEvent args)
        {
            if (args.Handled || !args.CanReach)
                return;

            if (args.Target is not {Valid: true} target ||
                !EntityManager.TryGetComponent(target, out RCDComponent? rcdComponent))
                return;

            if (rcdComponent.MaxAmmo - rcdComponent.CurrentAmmo < component.RefillAmmo)
            {
                _popupSystem.PopupEntity(Loc.GetString("rcd-ammo-component-after-interact-full-text"), rcdComponent.Owner, Filter.Entities(args.User));
                args.Handled = true;
                return;
            }

            rcdComponent.CurrentAmmo = Math.Min(rcdComponent.MaxAmmo, rcdComponent.CurrentAmmo + component.RefillAmmo);
            _popupSystem.PopupEntity(Loc.GetString("rcd-ammo-component-after-interact-refilled-text"), rcdComponent.Owner, Filter.Entities(args.User));
            EntityManager.QueueDeleteEntity(uid);

            args.Handled = true;
        }
    }
}
