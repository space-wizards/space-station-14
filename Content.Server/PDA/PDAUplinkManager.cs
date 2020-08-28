using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Mobs;
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
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private List<UplinkAccount> _accounts;
        private Dictionary<string, UplinkListingData> _listings;

        public IReadOnlyDictionary<string, UplinkListingData> FetchListings => _listings;

        public void Initialize()
        {
            _listings = new Dictionary<string, UplinkListingData>();
            foreach (var item in _prototypeManager.EnumeratePrototypes<UplinkStoreListingPrototype>())
            {
                var newListing = new UplinkListingData(item.ListingName, item.ItemId, item.Price, item.Category,
                    item.Description);

                RegisterUplinkListing(newListing);
            }

            _accounts = new List<UplinkAccount>();
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

            if (entity.TryGetComponent(out MindComponent mindComponent))
            {
                if (!mindComponent.HasMind)
                {
                    return false;
                }

                if (mindComponent.Mind!.AllRoles.Any(role => !role.Antagonist))
                {
                    return false;
                }
            }

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

        public bool TryPurchaseItem(UplinkAccount acc, string itemId)
        {
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

            var player = _entityManager.GetEntity(acc.AccountHolder);
            var hands = player.GetComponent<HandsComponent>();
            hands.PutInHandOrDrop(_entityManager.SpawnEntity(listing.ItemId,
                player.Transform.GridPosition).GetComponent<ItemComponent>());
            return true;
        }
    }
}
