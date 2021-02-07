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
                Owner.PopupMessage(eventArgs.User, Loc.GetString("The machine buzzes, and displays: \"USER PDA OUT OF RANGE (0039)\""));
                return false;
            }

            if (!eventArgs.User.TryGetComponent<MindComponent>(out var userMindComponent))
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("The machine buzzes, and displays: \"AUTHENTICATION FAILED (0045)\""));
                return false;
            }

            var userMind = userMindComponent.Mind;
            if (userMind == null)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("The machine buzzes, and displays: \"AUTHENTICATION FAILED (0052)\""));
                return false;
            }

            if (!eventArgs.Using.TryGetComponent<PDAComponent>(out var victimPDA))
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("The machine buzzes, and displays: \"GIVEN PDA IS NOT A PDA (0058)\""));
                return false;
            }

            if (!eventArgs.Using.TryGetComponent<TraitorDeathMatchReliableOwnerTagComponent>(out var victimPDAOwner))
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("The machine buzzes, and displays: \"GIVEN PDA HAS NO OWNER (0064)\""));
                return false;
            }

            if (victimPDAOwner.UserId == userMind.UserId)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("The machine buzzes, and displays: \"GIVEN PDA OWNED BY USER (0070)\""));
                return false;
            }

            var userPDAEntity = userInv.GetSlotItem(EquipmentSlotDefines.Slots.IDCARD)?.Owner;
            PDAComponent? userPDA = null;

            if (userPDAEntity != null)
                if (userPDAEntity.TryGetComponent<PDAComponent>(out var userPDAComponent))
                    userPDA = userPDAComponent;

            if (userPDA == null)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("The machine buzzes, and displays: \"NO USER PDA IN IDCARD POCKET (0083)\""));
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
                Owner.PopupMessage(eventArgs.User, Loc.GetString("The machine buzzes, and displays: \"USER PDA HAS NO UPLINK ACCOUNT (0102)\""));
                return false;
            }

            if (victimAccount == null)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("The machine buzzes, and displays: \"GIVEN PDA HAS NO UPLINK ACCOUNT (0108)\""));
                return false;
            }

            // 4 is the per-PDA bonus amount.
            var transferAmount = victimAccount.Balance + 4;
            victimAccount.ModifyAccountBalance(0);
            userAccount.ModifyAccountBalance(userAccount.Balance + transferAmount);

            victimPDA.Owner.Delete();

            Owner.PopupMessage(eventArgs.User, Loc.GetString("The machine plays a happy little tune, and displays: \"SUCCESS: {0} TC TRANSFERRED\"", transferAmount));
            return true;
        }
    }
}
