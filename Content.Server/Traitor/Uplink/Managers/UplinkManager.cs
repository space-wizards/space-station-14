using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Server.Mind.Components;
using Content.Shared.PDA;
using Content.Shared.Traitor.Uplink;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.PDA.Managers
{
    public class UplinkManager : IUplinkManager
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private readonly Dictionary<EntityUid, UplinkAccount> _accounts = new();
        private readonly Dictionary<string, UplinkListingData> _listings = new();

        public IReadOnlyDictionary<string, UplinkListingData> FetchListings => _listings;

        public void Initialize()
        {
            foreach (var item in _prototypeManager.EnumeratePrototypes<UplinkStoreListingPrototype>())
            {
                var newListing = new UplinkListingData(item.ListingName, item.ItemId, item.Price, item.Category,
                    item.Description);

                RegisterUplinkListing(newListing);
            }
        }

        private void RegisterUplinkListing(UplinkListingData listing)
        {
            if (!ContainsListing(listing))
            {
                _listings.Add(listing.ItemId, listing);
            }
        }

        private bool ContainsListing(UplinkListingData listing)
        {
            return _listings.ContainsKey(listing.ItemId);
        }

        public bool AddNewAccount(UplinkAccount acc)
        {
            var entity = _entityManager.GetEntity(acc.AccountHolder);

            if (entity.TryGetComponent(out MindComponent? mindComponent) && !mindComponent.HasMind)
            {
                return false;
            }

            if (_accounts.ContainsKey(acc.AccountHolder))
            {
                return false;
            }

            _accounts.Add(acc.AccountHolder, acc);
            return true;
        }

        public bool TryGetAccount(EntityUid owner, out UplinkAccount? acc)
        {
            return _accounts.TryGetValue(owner, out acc);
        }

        public bool ChangeBalance(UplinkAccount acc, int amt)
        {
            var account = _accounts.GetValueOrDefault(acc.AccountHolder);

            if (account == null)
            {
                return false;
            }

            if (account.Balance + amt < 0)
            {
                return false;
            }

            account.ModifyAccountBalance(account.Balance + amt);

            return true;
        }

        public bool TryPurchaseItem(UplinkAccount? acc, string itemId, EntityCoordinates spawnCoords, [NotNullWhen(true)] out IEntity? purchasedItem)
        {
            purchasedItem = null;
            if (acc == null)
            {
                return false;
            }

            if (!_listings.TryGetValue(itemId, out var listing))
            {
                return false;
            }

            if (acc.Balance < listing.Price)
            {
                return false;
            }

            if (!ChangeBalance(acc, -listing.Price))
            {
                return false;
            }

            purchasedItem = _entityManager.SpawnEntity(listing.ItemId, spawnCoords);
            return true;
        }
    }
}
