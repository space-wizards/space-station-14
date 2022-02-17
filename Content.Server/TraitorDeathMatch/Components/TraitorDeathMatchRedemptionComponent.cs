using System.Threading.Tasks;
using Content.Server.Mind.Components;
using Content.Server.Traitor.Uplink.Account;
using Content.Server.Traitor.Uplink.Components;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using InventoryComponent = Content.Shared.Inventory.InventoryComponent;

namespace Content.Server.TraitorDeathMatch.Components
{
    [RegisterComponent]
    public sealed class TraitorDeathMatchRedemptionComponent : Component, IInteractUsing
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!_entMan.TryGetComponent<MindComponent?>(eventArgs.User, out var userMindComponent))
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("traitor-death-match-redemption-component-interact-using-main-message",
                                                                 ("secondMessage", Loc.GetString("traitor-death-match-redemption-component-interact-using-no-mind-message"))));
                return false;
            }

            var userMind = userMindComponent.Mind;
            if (userMind == null)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("traitor-death-match-redemption-component-interact-using-main-message",
                                                                 ("secondMessage", Loc.GetString("traitor-death-match-redemption-component-interact-using-no-user-mind-message"))));
                return false;
            }

            if (!_entMan.TryGetComponent<UplinkComponent?>(eventArgs.Using, out var victimUplink))
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("traitor-death-match-redemption-component-interact-using-main-message",
                                                                 ("secondMessage", Loc.GetString("traitor-death-match-redemption-component-interact-using-no-pda-message"))));
                return false;
            }

            if (!_entMan.TryGetComponent<TraitorDeathMatchReliableOwnerTagComponent?>(eventArgs.Using, out var victimPDAOwner))
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("traitor-death-match-redemption-component-interact-using-main-message",
                                                                 ("secondMessage", Loc.GetString("traitor-death-match-redemption-component-interact-using-no-pda-owner-message"))));
                return false;
            }

            if (victimPDAOwner.UserId == userMind.UserId)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("traitor-death-match-redemption-component-interact-using-main-message",
                                                                 ("secondMessage", Loc.GetString("traitor-death-match-redemption-component-interact-using-pda-different-user-message"))));
                return false;
            }

            UplinkComponent? userUplink = null;
            var invSystem = EntitySystem.Get<InventorySystem>();

            if (invSystem.TryGetSlotEntity(eventArgs.User, "id", out var pdaUid) &&
                _entMan.TryGetComponent<UplinkComponent>(pdaUid, out var userUplinkComponent))
            {
                userUplink = userUplinkComponent;
            }

            if (userUplink == null)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("traitor-death-match-redemption-component-interact-using-main-message",
                                                                 ("secondMessage", Loc.GetString("traitor-death-match-redemption-component-interact-using-no-pda-in-pocket-message"))));
                return false;
            }

            // We have finally determined both PDA components. FINALLY.

            var userAccount = userUplink.UplinkAccount;
            var victimAccount = victimUplink.UplinkAccount;

            if (userAccount == null)
            {
                // This shouldn't even BE POSSIBLE in the actual mode this is meant for.
                // Advanced Syndicate anti-tampering technology.
                // Owner.PopupMessage(eventArgs.User, Loc.GetString("traitor-death-match-redemption-component-interact-using-tampering-detected"));
                // if (eventArgs.User.TryGetComponent<DamagableComponent>(out var userDamagable))
                //     userDamagable.ChangeDamage(DamageType.Shock, 9001, true, null);
                // ...So apparently, "it probably shouldn't kill people for a mistake".
                // :(
                // Give boring error message instead.
                Owner.PopupMessage(eventArgs.User, Loc.GetString("traitor-death-match-redemption-component-interact-using-main-message",
                                                                 ("secondMessage", Loc.GetString("traitor-death-match-redemption-component-interact-using-user-no-uplink-account-message"))));
                return false;
            }

            if (victimAccount == null)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("traitor-death-match-redemption-component-interact-using-main-message",
                                                                 ("secondMessage", Loc.GetString("traitor-death-match-redemption-component-interact-using-victim-no-uplink-account-message"))));
                return false;
            }

            // 4 is the per-PDA bonus amount.
            var accounts = _entMan.EntitySysManager.GetEntitySystem<UplinkAccountsSystem>();
            var transferAmount = victimAccount.Balance + 4;
            accounts.SetBalance(victimAccount, 0);
            accounts.AddToBalance(userAccount, transferAmount);

            _entMan.DeleteEntity(victimUplink.Owner);

            Owner.PopupMessage(eventArgs.User, Loc.GetString("traitor-death-match-redemption-component-interact-using-success-message", ("tcAmount", transferAmount)));
            return true;
        }
    }
}
