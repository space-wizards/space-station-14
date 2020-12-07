using System.Collections.Generic;
using Content.Server.GameObjects.Components.Cargo;
using Content.Shared.Prototypes.Cargo;

namespace Content.Server.Cargo
{
    public class CargoOrderDataManager : ICargoOrderDataManager
    {
        private readonly Dictionary<int, CargoOrderDatabase> _accounts = new();
        private readonly List<CargoOrderDatabaseComponent> _components = new();

        public CargoOrderDataManager()
        {
            CreateAccount(0);
        }

        public void CreateAccount(int id)
        {
            _accounts.Add(id, new CargoOrderDatabase(id));
        }

        public bool TryGetAccount(int id, out CargoOrderDatabase account)
        {
            if (_accounts.TryGetValue(id, out var _account))
            {
                account = _account;
                return true;
            }
            account = null;
            return false;
        }

        /// <summary>
        ///     Adds an order to the database.
        /// </summary>
        /// <param name="requester">The person who requested the item.</param>
        /// <param name="reason">The reason the product was requested.</param>
        /// <param name="productId">The ID of the product requested.</param>
        /// <param name="amount">The amount of the products requested.</param>
        /// <param name="payingAccountId">The ID of the bank account paying for the order.</param>
        /// <param name="approved">Whether the order will be bought when the orders are processed.</param>
        public virtual void AddOrder(int id, string requester, string reason, string productId, int amount, int payingAccountId)
        {
            if (amount < 1 || !TryGetAccount(id, out var account))
                return;
            account.AddOrder(requester, reason, productId, amount, payingAccountId);
            SyncComponentsWithId(id);
        }

        public void RemoveOrder(int id, int orderNumber)
        {
            if (!TryGetAccount(id, out var account))
                return;
            account.RemoveOrder(orderNumber);
            SyncComponentsWithId(id);
        }

        public void ApproveOrder(int id, int orderNumber)
        {
            if (!TryGetAccount(id, out var account))
                return;
            account.ApproveOrder(orderNumber);
            SyncComponentsWithId(id);
        }

        private void SyncComponentsWithId(int id)
        {
            foreach (var component in _components)
            {
                if (!component.ConnectedToDatabase || component.Database.Id != id)
                    continue;
                component.Dirty();
            }
        }

        public List<CargoOrderData> RemoveAndGetApprovedFrom(CargoOrderDatabase database)
        {
            var approvedOrders = database.SpliceApproved();
            SyncComponentsWithId(database.Id);
            return approvedOrders;
        }

        public void AddComponent(CargoOrderDatabaseComponent component)
        {
            if (_components.Contains(component))
                return;
            _components.Add(component);
            component.Database = _accounts[0];
        }

        public List<CargoOrderData> GetOrdersFromAccount(int accountId)
        {
            if (!TryGetAccount(accountId, out var account))
                return null;
            return account.GetOrders();
        }

        public (int CurrentCapacity, int MaxCapacity) GetCapacity(int id)
        {
            TryGetAccount(id, out var account);
            return (account.CurrentOrderSize, account.MaxOrderSize);
        }
    }
}
