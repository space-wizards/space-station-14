using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Server.Mind.Components;
using Content.Shared.Traitor.Uplink;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.Traitor.Uplink.Account
{
    /// <summary>
    ///     Manage all registred uplink accounts and their balance
    /// </summary>
    public class UplinkAccountsSystem : EntitySystem
    {
        [Dependency]
        private readonly UplinkListingSytem _listingSystem = default!;

        private readonly HashSet<UplinkAccount> _accounts = new();

        public bool AddNewAccount(UplinkAccount acc)
        {
            return _accounts.Add(acc);
        }

        public bool ChargeBalance(UplinkAccount account, int amt)
        {
            if (account.Balance + amt < 0)
            {
                return false;
            }

            account.Balance += amt;

            RaiseLocalEvent(new UplinkAccountBalanceChanged(account, amt));
            return true;
        }

        public bool TryPurchaseItem(UplinkAccount acc, string itemId, EntityCoordinates spawnCoords, [NotNullWhen(true)] out IEntity? purchasedItem)
        {
            purchasedItem = null;

            if (!_listingSystem.TryGetListing(itemId, out var listing))
            {
                return false;
            }

            if (acc.Balance < listing.Price)
            {
                return false;
            }

            if (!ChargeBalance(acc, -listing.Price))
            {
                return false;
            }

            purchasedItem = EntityManager.SpawnEntity(listing.ItemId, spawnCoords);
            return true;
        }
    }
}
