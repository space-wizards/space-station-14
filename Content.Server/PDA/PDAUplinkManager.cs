using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects;
using Content.Server.Interfaces.PDA;
using Content.Shared.GameObjects.Components.PDA;
using Content.Shared.Prototypes.PDA;
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

            _accounts = new List<UplinkAccount>();
        }

        private void RegisterUplinkListing(UplinkListingData listing)
        {
            if (!ContainsListing(listing))
            {
                _listings.Add(listing);
            }

        }

        private bool ContainsListing(UplinkListingData listing)
        {
            if (_listings.Any(otherListing => listing.Equals(otherListing)))
            {
                return true;
            }

            return false;
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
            if (!ContainsListing(listing) || acc.Balance < listing.Price)
            {
                return false;
            }

            var player = _entityManager.GetEntity(acc.AccountHolder);
            var hands = player.GetComponent<HandsComponent>();
            hands.PutInHandOrDrop(_entityManager.SpawnEntity(listing.ItemID,
                player.Transform.GridPosition).GetComponent<ItemComponent>());
            return ChangeBalance(acc, listing.Price);

        }



    }
}
