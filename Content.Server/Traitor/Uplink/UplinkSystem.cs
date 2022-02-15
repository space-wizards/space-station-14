using System.Linq;
using Content.Server.Hands.Components;
using Content.Server.Traitor.Uplink.Account;
using Content.Server.Traitor.Uplink.Components;
using Content.Server.UserInterface;
using Content.Shared.ActionBlocker;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.PDA;
using Content.Shared.Traitor.Uplink;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Player;

namespace Content.Server.Traitor.Uplink
{
    public class UplinkSystem : EntitySystem
    {
        [Dependency]
        private readonly UplinkAccountsSystem _accounts = default!;
        [Dependency]
        private readonly UplinkListingSytem _listing = default!;

        [Dependency] private readonly InventorySystem _inventorySystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<UplinkComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<UplinkComponent, ComponentRemove>(OnRemove);
            SubscribeLocalEvent<UplinkComponent, ActivateInWorldEvent>(OnActivate);

            // UI events
            SubscribeLocalEvent<UplinkComponent, UplinkBuyListingMessage>(OnBuy);
            SubscribeLocalEvent<UplinkComponent, UplinkRequestUpdateInterfaceMessage>(OnRequestUpdateUI);
            SubscribeLocalEvent<UplinkComponent, UplinkTryWithdrawTC>(OnWithdrawTC);

            SubscribeLocalEvent<UplinkAccountBalanceChanged>(OnBalanceChangedBroadcast);
        }

        public void SetAccount(UplinkComponent component, UplinkAccount account)
        {
            if (component.UplinkAccount != null)
            {
                Logger.Error("Can't init one uplink with different account!");
                return;
            }

            component.UplinkAccount = account;
        }

        private void OnInit(EntityUid uid, UplinkComponent component, ComponentInit args)
        {
            RaiseLocalEvent(uid, new UplinkInitEvent(component));

            // if component has a preset info (probably spawn by admin)
            // create a new account and register it for this uplink
            if (component.PresetInfo != null)
            {
                var account = new UplinkAccount(component.PresetInfo.StartingBalance);
                _accounts.AddNewAccount(account);
                SetAccount(component, account);
            }
        }

        private void OnRemove(EntityUid uid, UplinkComponent component, ComponentRemove args)
        {
            RaiseLocalEvent(uid, new UplinkRemovedEvent());
        }

        private void OnActivate(EntityUid uid, UplinkComponent component, ActivateInWorldEvent args)
        {
            if (args.Handled)
                return;

            // check if uplinks activates directly or use some proxy, like a PDA
            if (!component.ActivatesInHands)
                return;
            if (component.UplinkAccount == null)
                return;

            if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
                return;

            ToggleUplinkUI(component, actor.PlayerSession);
            args.Handled = true;
        }

        private void OnBalanceChangedBroadcast(UplinkAccountBalanceChanged ev)
        {
            foreach (var uplink in EntityManager.EntityQuery<UplinkComponent>())
            {
                if (uplink.UplinkAccount == ev.Account)
                {
                    UpdateUserInterface(uplink);
                }
            }
        }

        private void OnRequestUpdateUI(EntityUid uid, UplinkComponent uplink, UplinkRequestUpdateInterfaceMessage args)
        {
            UpdateUserInterface(uplink);
        }

        private void OnBuy(EntityUid uid, UplinkComponent uplink, UplinkBuyListingMessage message)
        {
            if (message.Session.AttachedEntity is not {Valid: true} player) return;
            if (uplink.UplinkAccount == null) return;

            if (!_accounts.TryPurchaseItem(uplink.UplinkAccount, message.ItemId,
                EntityManager.GetComponent<TransformComponent>(player).Coordinates, out var entity))
            {
                SoundSystem.Play(Filter.SinglePlayer(message.Session), uplink.InsufficientFundsSound.GetSound(),
                    uplink.Owner, AudioParams.Default);
                RaiseNetworkEvent(new UplinkInsufficientFundsMessage(), message.Session.ConnectedClient);
                return;
            }

            if (EntityManager.TryGetComponent(player, out HandsComponent? hands) &&
                EntityManager.TryGetComponent(entity.Value, out SharedItemComponent? item))
            {
                hands.PutInHandOrDrop(item);
            }

            SoundSystem.Play(Filter.SinglePlayer(message.Session), uplink.BuySuccessSound.GetSound(),
                uplink.Owner, AudioParams.Default.WithVolume(-8f));

            RaiseNetworkEvent(new UplinkBuySuccessMessage(), message.Session.ConnectedClient);
        }

        private void OnWithdrawTC(EntityUid uid, UplinkComponent uplink, UplinkTryWithdrawTC args)
        {
            var acc = uplink.UplinkAccount;
            if (acc == null)
                return;

            if (args.Session.AttachedEntity is not {Valid: true} player) return;
            var cords = EntityManager.GetComponent<TransformComponent>(player).Coordinates;

            // try to withdraw TCs from account
            if (!_accounts.TryWithdrawTC(acc, args.TC, cords, out var tcUid))
                return;

            // try to put it into players hands
            if (EntityManager.TryGetComponent(player, out SharedHandsComponent? hands))
                hands.TryPutInAnyHand(tcUid.Value);

            // play buying sound
            SoundSystem.Play(Filter.SinglePlayer(args.Session), uplink.BuySuccessSound.GetSound(),
                    uplink.Owner, AudioParams.Default.WithVolume(-8f));

            UpdateUserInterface(uplink);
        }

        public void ToggleUplinkUI(UplinkComponent component, IPlayerSession session)
        {
            var ui = component.Owner.GetUIOrNull(UplinkUiKey.Key);
            ui?.Toggle(session);

            UpdateUserInterface(component);
        }

        private void UpdateUserInterface(UplinkComponent component)
        {
            var ui = component.Owner.GetUIOrNull(UplinkUiKey.Key);
            if (ui == null)
                return;

            var listings = _listing.GetListings().Values.ToArray();
            var acc = component.UplinkAccount;

            UplinkAccountData accData;
            if (acc != null)
                accData = new UplinkAccountData(acc.AccountHolder, acc.Balance);
            else
                accData = new UplinkAccountData(null, 0);

            ui.SetState(new UplinkUpdateState(accData, listings));
        }

        public bool AddUplink(EntityUid user, UplinkAccount account, EntityUid? uplinkEntity = null)
        {
            // Try to find target item
            if (uplinkEntity == null)
            {
                uplinkEntity = FindUplinkTarget(user);
                if (uplinkEntity == null)
                    return false;
            }

            var uplink = uplinkEntity.Value.EnsureComponent<UplinkComponent>();
            SetAccount(uplink, account);

            return true;
        }

        private EntityUid? FindUplinkTarget(EntityUid user)
        {
            // Try to find PDA in inventory

            if (_inventorySystem.TryGetContainerSlotEnumerator(user, out var containerSlotEnumerator))
            {
                while (containerSlotEnumerator.MoveNext(out var pdaUid))
                {
                    if(!pdaUid.ContainedEntity.HasValue) continue;

                    if(HasComp<PDAComponent>(pdaUid.ContainedEntity.Value))
                        return pdaUid.ContainedEntity.Value;
                }
            }

            // Also check hands
            if (EntityManager.TryGetComponent(user, out HandsComponent? hands))
            {
                var heldItems = hands.GetAllHeldItems();
                foreach (var item in heldItems)
                {
                    if (EntityManager.HasComponent<PDAComponent>(item.Owner))
                        return item.Owner;
                }
            }

            return null;
        }
    }
}
