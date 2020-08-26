using System.Collections.Generic;
using Content.Shared.GameObjects.Components.PDA;

namespace Content.Server.Interfaces.PDA
{
    public interface IPDAUplinkManager
    {
        public IReadOnlyDictionary<string, UplinkListingData> FetchListings => null;

        void Initialize();

        public bool AddNewAccount(UplinkAccount acc);

        public bool ChangeBalance(UplinkAccount acc, int amt);

        public bool TryPurchaseItem(UplinkAccount acc, string itemId);

    }
}
