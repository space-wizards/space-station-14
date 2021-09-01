using Content.Server.PDA.Managers;
using Content.Server.Traitor.Uplink.Components;
using Content.Server.UserInterface;
using Content.Shared.Traitor.Uplink;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using System;
using System.Linq;

namespace Content.Server.Traitor.Uplink.Systems
{
    public class UplinkSystem : EntitySystem
    {
        [Dependency] private readonly IUplinkManager _uplinkManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<UplinkComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<UplinkComponent, ComponentRemove>(OnRemove);
            SubscribeLocalEvent<UplinkComponent, ShowUplinkUIAttempt>(OnShowUI);
        }

        private void OnInit(EntityUid uid, UplinkComponent component, ComponentInit args)
        {
            RaiseLocalEvent(uid, new UplinkInitEvent(component));
        }

        private void OnRemove(EntityUid uid, UplinkComponent component, ComponentRemove args)
        {
            RaiseLocalEvent(uid, new UplinkRemovedEvent());
        }

        private void OnShowUI(EntityUid uid, UplinkComponent component, ShowUplinkUIAttempt args)
        {
            var ui = component.Owner.GetUIOrNull(UplinkUiKey.Key);
            ui?.Toggle(args.Session);

            UpdatePDAUserInterface(component);
        }

        private void UpdatePDAUserInterface(UplinkComponent component)
        {
            var ui = component.Owner.GetUIOrNull(UplinkUiKey.Key);
            if (ui == null)
                return;

            var listings = _uplinkManager.FetchListings.Values.ToArray();
            var acc = component.UplinkAccount;

            UplinkAccountData accData;
            if (acc != null)
                accData = new UplinkAccountData(acc.AccountHolder, acc.Balance);
            else
                accData = new UplinkAccountData(null, 0);

            ui.SetState(new UplinkUpdateState(accData, listings));
        }
    }
}
