using Content.Server.PDA;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.Interfaces.PDA
{
    public interface IPDAUplinkManager
    {
        void Initialize();
        public bool AddNewAccount(UplinkAccount acc);

        public bool ChangeBalance(UplinkAccount acc, int amt);

        public bool PurchaseItem(UplinkAccount acc, UplinkStoreListing listing);

    }
}
