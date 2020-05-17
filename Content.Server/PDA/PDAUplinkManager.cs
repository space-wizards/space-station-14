using System.Collections.Generic;
using Content.Server.Interfaces.PDA;
using Robust.Shared.GameObjects;
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

        public void Initialize()
        {
            //throw new System.NotImplementedException();
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
            if (acc.Balance < listing.Price)
            {
                return false;
            }

            var player = _entityManager.GetEntity(acc.AccountHolder);
            _entityManager.SpawnEntity(listing.Item.ID,
                player.Transform.GridPosition);
            return true;

        }
    }

    public struct UplinkAccount
    {
        public EntityUid AccountHolder;
        public int Balance;
    }

    public struct UplinkStoreListing
    {
        public EntityPrototype Item;
        public int Price;
    }
}
