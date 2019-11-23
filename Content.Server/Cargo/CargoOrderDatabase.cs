using Content.Shared.Prototypes.Cargo;
using System.Collections.Generic;
using System.Linq;

namespace Content.Server.Cargo
{
    public class CargoOrderDatabase
    {
        private Dictionary<int, CargoOrderData> _orders = new Dictionary<int, CargoOrderData>();
        private int _orderNumber = 0;

        public CargoOrderDatabase(int id)
        {
            Id = id;
        }

        public int Id { get; private set; }

        /// <summary>
        ///     Removes all orders from the database.
        /// </summary>
        public void Clear()
        {
            _orders.Clear();
        }

        /// <summary>
        ///     Returns a list of all orders.
        /// </summary>
        /// <returns>A list of orders</returns>
        public List<CargoOrderData> GetOrders()
        {
            return _orders.Values.ToList();
        }

        public bool TryGetOrder(int id, out CargoOrderData order)
        {
            if (_orders.TryGetValue(id, out var _order))
            {
                order = _order;
                return true;
            }
            order = null;
            return false;
        }

        public List<CargoOrderData> SpliceApproved()
        {
            var orders = _orders.Values.Where(order => order.Approved).ToList();
            foreach (var order in orders)
                _orders.Remove(order.OrderNumber);
            return orders;
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
        public void AddOrder(string requester, string reason, string productId, int amount, int payingAccountId)
        {
            var order = new CargoOrderData(_orderNumber, requester, reason, productId, amount, payingAccountId);
            if (Contains(order))
                return;
            _orders.Add(_orderNumber, order);
            _orderNumber += 1;
        }

        /// <summary>
        ///     Removes an order from the database.
        /// </summary>
        /// <param name="order">The order to be removed.</param>
        /// <returns>Whether it could be removed or not</returns>
        public bool RemoveOrder(int orderNumber)
        {
            return _orders.Remove(orderNumber);
        }
        
        /// <summary>
        ///     Approves an order in the database.
        /// </summary>
        /// <param name="order">The order to be approved.</param>
        public void ApproveOrder(int orderNumber)
        {
            if (!_orders.TryGetValue(orderNumber, out var order))
                return;
            order.Approved = true;
        }

        /// <summary>
        ///     Returns whether the database contains the order or not.
        /// </summary>
        /// <param name="order">The order to check</param>
        /// <returns>Whether the database contained the order or not.</returns>
        public bool Contains(CargoOrderData order)
        {
            return _orders.ContainsValue(order);
        }
    }
}
