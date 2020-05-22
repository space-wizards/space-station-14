using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.PDA;
using Content.Shared.Prototypes.PDA;

namespace Content.Server.Interfaces.PDA
{
    public interface IPDAUplinkManager
    {
        public IReadOnlyList<UplinkListingData> FetchListings => null;
        void Initialize();
        public bool AddNewAccount(UplinkAccount acc);

        public bool ChangeBalance(UplinkAccount acc, int amt);

        public bool TryPurchaseItem(UplinkAccount acc, UplinkListingData listing);

    }
}
