using System.Collections.Generic;
using System.IO;
using System.Linq;
using Content.Server.Interfaces.PDA;
using Content.Shared.GameObjects.Components.PDA;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Server.PDA
{
    public class PDAUplinkManager : IPDAUplinkManager
    {
#pragma warning disable 649
        [Dependency] private readonly IPrototypeManager _prototypeManager;
        [Dependency] private readonly IEntityManager _entityManager;
        [Dependency] private readonly IResourceManager _resourceMan;
#pragma warning restore 649
        private List<UplinkAccount> _accounts;
        private List<UplinkStoreListing> _listings;

        public IReadOnlyList<UplinkStoreListing> FetchListings()
        {
            return _listings;
        }

        public void Initialize()
        {
            var path = new ResourcePath("/uplink_catalog.yml");
            if (_resourceMan.ContentFileExists(path))
            {
                LoadCatalog(path);
            }
        }

        private void LoadCatalog(ResourcePath yamlFile)
        {
            YamlDocument document;
            using (var stream = _resourceMan.ContentFileRead(yamlFile))
            using (var reader = new StreamReader(stream, EncodingHelpers.UTF8))
            {
                var yamlStream = new YamlStream();
                yamlStream.Load(reader);
                document = yamlStream.Documents[0];
            }

            var mapping = (YamlMappingNode) document.RootNode;
            foreach (var keyMapping in mapping.GetNode<YamlSequenceNode>("listings").Cast<YamlMappingNode>())
            {
                var category = keyMapping.GetNode("category").AsString();
                var itemID = keyMapping.GetNode("itemID").AsString();
                var price = keyMapping.GetNode("price").AsInt();
                if (!_prototypeManager.TryIndex(itemID, out EntityPrototype prototype))
                {
                    continue;
                }
                var description = keyMapping
                    .TryGetNode("description", out var desc)
                    ? desc.AsString() : prototype.Description;

                var newListing = new UplinkStoreListing
                {
                    Item = prototype,
                    Price = price,
                    Category = category,
                    Description = description
                };

                RegisterUplinkListing(newListing);
            }
        }


        private void RegisterUplinkListing(UplinkStoreListing listing)
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

        public bool PurchaseItem(UplinkAccount acc, UplinkStoreListing listing)
        {
            if (acc.Balance < listing.Price || !_listings.Contains(listing))
            {
                return false;
            }

            var player = _entityManager.GetEntity(acc.AccountHolder);
            _entityManager.SpawnEntity(listing.Item.ID,
                player.Transform.GridPosition);
            return true;

        }


    }
}
