using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Server.PDA.Managers;
using Content.Server.Traitor.Uplink.Components;
using Content.Server.UserInterface;
using Content.Shared.Traitor.Uplink;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;
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

            var ui = component.Owner.GetUIOrNull(UplinkUiKey.Key);
            if (ui != null)
                ui.OnReceiveMessage += (msg) => OnUIMessage(component, msg);
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

        private void OnUIMessage(UplinkComponent uplink, ServerBoundUserInterfaceMessage message)
        {
            switch (message.Message)
            {
                case UplinkRequestUpdateInterfaceMessage _:
                    UpdatePDAUserInterface(uplink);
                    break;
                case UplinkBuyListingMessage buyMsg:
                    {
                        var player = message.Session.AttachedEntity;
                        if (player == null) break;

                        if (!_uplinkManager.TryPurchaseItem(uplink.UplinkAccount, buyMsg.ItemId,
                            player.Transform.Coordinates, out var entity))
                        {
                            SoundSystem.Play(Filter.SinglePlayer(message.Session), uplink.InsufficientFundsSound.GetSound(),
                                uplink.Owner, AudioParams.Default);
                            RaiseNetworkEvent(new UplinkInsufficientFundsMessage(), message.Session.ConnectedClient);
                            break;
                        }

                        if (player.TryGetComponent(out HandsComponent? hands) &&
                            entity.TryGetComponent(out ItemComponent? item))
                        {
                            hands.PutInHandOrDrop(item);
                        }

                        SoundSystem.Play(Filter.SinglePlayer(message.Session), uplink.BuySuccessSound.GetSound(),
                            uplink.Owner, AudioParams.Default.WithVolume(-2f));

                        RaiseNetworkEvent(new UplinkBuySuccessMessage(), message.Session.ConnectedClient);
                        break;
                    }

            }
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
