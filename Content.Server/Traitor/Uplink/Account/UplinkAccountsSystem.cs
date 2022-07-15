using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Traitor.Uplink.Components;
using Content.Shared.Stacks;
using Content.Shared.Traitor.Uplink;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Traitor.Uplink.Account
{
    /// <summary>
    ///     Manage all registred uplink accounts and their balance
    /// </summary>
    public sealed class UplinkAccountsSystem : EntitySystem
    {
        public const string TelecrystalProtoId = "Telecrystal";

        [Dependency]
        private readonly UplinkListingSytem _listingSystem = default!;
        [Dependency]
        private readonly SharedStackSystem _stackSystem = default!;

        private readonly HashSet<UplinkAccount> _accounts = new();

        public bool AddNewAccount(UplinkAccount acc)
        {
            return _accounts.Add(acc);
        }

        public bool HasAccount(EntityUid holder) =>
            _accounts.Any(acct => acct.AccountHolder == holder);

        /// <summary>
        /// Add TC to uplinks account balance
        /// </summary>
        public bool AddToBalance(UplinkAccount account, int toAdd)
        {
            account.Balance += toAdd;

            RaiseLocalEvent(new UplinkAccountBalanceChanged(account, toAdd));
            return true;
        }

        /// <summary>
        /// Charge TC from uplinks account balance
        /// </summary>
        public bool RemoveFromBalance(UplinkAccount account, int price)
        {
            if (account.Balance - price < 0)
                return false;

            account.Balance -= price;

            RaiseLocalEvent(new UplinkAccountBalanceChanged(account, -price));
            return true;
        }

        /// <summary>
        /// Force-set TC uplinks account balance to a new value
        /// </summary>
        public bool SetBalance(UplinkAccount account, int newBalance)
        {
            if (newBalance < 0)
                return false;

            var dif = newBalance - account.Balance;
            account.Balance = newBalance;
            RaiseLocalEvent(new UplinkAccountBalanceChanged(account, dif));
            return true;

        }

        public bool TryPurchaseItem(UplinkComponent component, string itemId, EntityCoordinates spawnCoords, [NotNullWhen(true)] out EntityUid? purchasedItem)
        {
            var acc = component.UplinkAccount;
            purchasedItem = null;

            if (acc == null) return false;
            if (acc.AccountHolder == null) return false;

            if (!_listingSystem.TryGetListing(itemId, out var listing))
            {
                return false;
            }

            if (acc.Balance < listing.Price)
            {
                return false;
            }

            if (!RemoveFromBalance(acc, listing.Price))
            {
                return false;
            }

            purchasedItem = EntityManager.SpawnEntity(listing.ItemId, spawnCoords);
            return true;
        }

        public bool TryWithdrawTC(UplinkAccount acc, int tc, EntityCoordinates spawnCoords, [NotNullWhen(true)] out EntityUid? stackUid)
        {
            stackUid = null;

            // try to charge TC from players account
            var actTC = Math.Min(tc, acc.Balance);
            if (actTC <= 0)
                return false;
            if (!RemoveFromBalance(acc, actTC))
                return false;

            // create a stack of TCs near player
            var stackEntity = EntityManager.SpawnEntity(TelecrystalProtoId, spawnCoords);
            stackUid = stackEntity;

            // set right amount in stack
            _stackSystem.SetCount(stackUid.Value, actTC);
            return true;
        }
    }
}
