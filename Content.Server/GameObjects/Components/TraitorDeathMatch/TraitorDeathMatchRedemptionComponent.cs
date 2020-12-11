#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.PDA;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Mobs;
using Content.Server.Mobs.Roles;
using Content.Server.Mobs.Roles.Suspicion;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Inventory;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.TraitorDeathMatch
{
    [RegisterComponent]
    public class TraitorDeathMatchRedemptionComponent : Component, IInteractUsing
    {
        /// <inheritdoc />
        public override string Name => "TraitorDeathMatchRedemption";

        public async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent<InventoryComponent>(out var userInv))
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("No inventory! How'd you manage that?"));
                return false;
            }

            if (!eventArgs.User.TryGetComponent<MindComponent>(out var userMindComponent))
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("You have no mind component! How'd you manage that?"));
                return false;
            }

            var userMind = userMindComponent.Mind;
            if (userMind == null)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("You have no mind! How'd you manage that?"));
                return false;
            }

            if (!eventArgs.Using.TryGetComponent<PDAComponent>(out var victimPDA))
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("It must be a PDA!"));
                return false;
            }

            if (!eventArgs.Using.TryGetComponent<TraitorDeathMatchReliableOwnerTagComponent>(out var victimPDAOwner))
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("This PDA isn't owned by anyone!"));
                return false;
            }

            if (victimPDAOwner.UserId == userMind.UserId)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("You can't redeem your own PDA!"));
                return false;
            }

            var userPDAEntity = userInv.GetSlotItem(EquipmentSlotDefines.Slots.IDCARD)?.Owner;
            PDAComponent? userPDA = null;

            if (userPDAEntity != null)
                if (userPDAEntity.TryGetComponent<PDAComponent>(out var userPDAComponent))
                    userPDA = userPDAComponent;

            if (userPDA == null)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("You must have your own PDA in your PDA slot."));
                return false;
            }

            // We have finally determined both PDA components. FINALLY.

            var userAccount = userPDA.SyndicateUplinkAccount;
            var victimAccount = victimPDA.SyndicateUplinkAccount;

            if (userAccount == null)
            {
                // This shouldn't even BE POSSIBLE in the actual mode this is meant for.
                // Advanced Syndicate anti-tampering technology.
                // Owner.PopupMessage(eventArgs.User, Loc.GetString("Tampering detected."));
                // if (eventArgs.User.TryGetComponent<DamagableComponent>(out var userDamagable))
                //     userDamagable.ChangeDamage(DamageType.Shock, 9001, true, null);
                // ...So apparently, "it probably shouldn't kill people for a mistake".
                // :(
                // Give boring error message instead.
                Owner.PopupMessage(eventArgs.User, Loc.GetString("Your own PDA does not have an uplink account."));
                return false;
            }

            if (victimAccount == null)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("The PDA you're trying to redeem does not have an uplink account."));
                return false;
            }

            // 4 is the per-PDA bonus amount.
            var transferAmount = victimAccount.Balance + 4;
            victimAccount.ModifyAccountBalance(0);
            userAccount.ModifyAccountBalance(userAccount.Balance + transferAmount);

            victimPDA.Owner.Delete();

            Owner.PopupMessage(eventArgs.User, Loc.GetString("You received {0} TC.", transferAmount));
            return true;
        }
    }
}
