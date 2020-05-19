using System.Collections.Generic;
using Content.Server.Interfaces.PDA;
using Content.Shared.GameObjects.Components.PDA;
using Content.Shared.Prototypes.PDA;
using Robust.Client.Placement.Modes;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Server.PDA
{
    public class PDAUplinkManager : IPDAUplinkManager
    {
#pragma warning disable 649
        [Dependency] private readonly IPrototypeManager _prototypeManager;
        [Dependency] private readonly IEntityManager _entityManager;
#pragma warning restore 649

        private List<UplinkAccount> _accounts;
        private List<UplinkListingData> _listings;

        public IReadOnlyList<UplinkListingData> FetchListings()
        {
            return _listings;
        }

        public void Initialize()
        {
            _listings = new List<UplinkListingData>();
            foreach (var item in _prototypeManager.EnumeratePrototypes<UplinkStoreListingPrototype>())
            {
                var newListing = new UplinkListingData(item.ListingName, item.ItemID, item.Price, item.Category,
                    item.Description);

                RegisterUplinkListing(newListing);
            }
        }

        private void RegisterUplinkListing(UplinkListingData listing)
        {
            if (_listings.Contains(listing))
            {
                return;
            }

            _listings.Add(listing);
        }

        public bool AddNewAccount(UplinkAccount acc)
        {
            if (_accounts.Contains(acc))
            {
                return false;
            }

            _accounts.Add(acc);
            return true;
        }

        public bool ChangeBalance(UplinkAccount acc, int amt)
        {
            var account = _accounts.Find(uplinkAccount => uplinkAccount.AccountHolder == acc.AccountHolder);
            if (account.Balance + amt < 0)
            {
                return false;
            }
            account.Balance -= amt;
            return true;
        }

        public bool PurchaseItem(UplinkAccount acc, UplinkListingData listing)
        {
            if (acc.Balance < listing.Price || !_listings.Contains(listing))
            {
                return false;
            }

            var player = _entityManager.GetEntity(acc.AccountHolder);
            _entityManager.SpawnEntity(listing.ItemID,
                player.Transform.GridPosition);
            return true;

        }


    }
}
