using System.Collections.Generic;
using Content.Shared.GameObjects.Components.PDA;

namespace Content.Server.Interfaces.PDA
{
    public interface IPDAUplinkManager
    {
        public IReadOnlyList<UplinkStoreListing> FetchListings();
        void Initialize();
        public bool AddNewAccount(UplinkAccount acc);

        public bool ChangeBalance(UplinkAccount acc, int amt);

        public bool PurchaseItem(UplinkAccount acc, UplinkStoreListing listing);

    }
}
