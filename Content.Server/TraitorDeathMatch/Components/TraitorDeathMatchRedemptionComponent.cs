using System.Threading.Tasks;
using Content.Server.Inventory.Components;
using Content.Server.Mind.Components;
using Content.Server.PDA;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.TraitorDeathMatch.Components
{
    [RegisterComponent]
    public class TraitorDeathMatchRedemptionComponent : Component, IInteractUsing
    {
        /// <inheritdoc />
        public override string Name => "TraitorDeathMatchRedemption";

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent<InventoryComponent>(out var userInv))
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("traitor-death-match-redemption-component-interact-using-main-message",
                                                                 ("secondMessage", Loc.GetString("traitor-death-match-redemption-component-interact-using-no-inventory-message"))));
                return false;
            }

            if (!eventArgs.User.TryGetComponent<MindComponent>(out var userMindComponent))
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

            if (!eventArgs.Using.TryGetComponent<PDAComponent>(out var victimPDA))
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("traitor-death-match-redemption-component-interact-using-main-message",
                                                                 ("secondMessage", Loc.GetString("traitor-death-match-redemption-component-interact-using-no-pda-message"))));
                return false;
            }

            if (!eventArgs.Using.TryGetComponent<TraitorDeathMatchReliableOwnerTagComponent>(out var victimPDAOwner))
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

            var userPDAEntity = userInv.GetSlotItem(EquipmentSlotDefines.Slots.IDCARD)?.Owner;
            PDAComponent? userPDA = null;

            if (userPDAEntity != null)
                if (userPDAEntity.TryGetComponent<PDAComponent>(out var userPDAComponent))
                    userPDA = userPDAComponent;

            if (userPDA == null)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("traitor-death-match-redemption-component-interact-using-main-message",
                                                                 ("secondMessage", Loc.GetString("traitor-death-match-redemption-component-interact-using-no-pda-in-pocket-message"))));
                return false;
            }

            // We have finally determined both PDA components. FINALLY.

            var userAccount = userPDA.SyndicateUplinkAccount;
            var victimAccount = victimPDA.SyndicateUplinkAccount;

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
            var transferAmount = victimAccount.Balance + 4;
            victimAccount.ModifyAccountBalance(0);
            userAccount.ModifyAccountBalance(userAccount.Balance + transferAmount);

            victimPDA.Owner.Delete();

            Owner.PopupMessage(eventArgs.User, Loc.GetString("traitor-death-match-redemption-component-interact-using-success-message", ("tcAmount", transferAmount)));
            return true;
        }
    }
}
