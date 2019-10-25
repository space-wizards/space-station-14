using Content.Server.GameObjects.Components.Cargo;
using Content.Shared.Prototypes.Cargo;
using System.Collections.Generic;

namespace Content.Server.Cargo
{
    public class CargoOrderDatabase
    {
        private List<CargoOrderData> _orders = new List<CargoOrderData>();
        private int _orderNumber = 0;

        public CargoOrderDatabase(int id)
        {
            Id = id;
        }

        public int Id { get; private set; }
        public List<CargoOrderData> Orders => _orders;

        public IEnumerator<CargoOrderData> GetEnumerator()
        {
            return _orders.GetEnumerator();
        }

        /// <summary>
        ///     Removes all orders from the database.
        /// </summary>
        public virtual void Clear()
        {
            _orders.Clear();
        }

        /// <summary>
        ///     Adds an order to the database.
        /// </summary>
        /// <param name="order">The order to be added.</param>
        public virtual void AddOrder(CargoOrderData order)
        {
            if (!Contains(order))
                _orders.Add(order);
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
        public virtual void AddOrder(string requester, string reason, string productId, int amount, int payingAccountId, bool approved)
        {
            var order = new CargoOrderData(_orderNumber, requester, reason, productId, amount, payingAccountId, approved);
            _orderNumber += 1;
            if (!Contains(order))
                _orders.Add(order);
        }

        /// <summary>
        ///     Removes an order from the database.
        /// </summary>
        /// <param name="order">The order to be removed.</param>
        /// <returns>Whether it could be removed or not</returns>
        public virtual bool RemoveOrder(CargoOrderData order)
        {
            return _orders.Remove(order);
        }

        /// <summary>
        ///     Returns whether the database contains the order or not.
        /// </summary>
        /// <param name="order">The order to check</param>
        /// <returns>Whether the database contained the order or not.</returns>
        public virtual bool Contains(CargoOrderData order)
        {
            return _orders.Contains(order);
        }

        /// <summary>
        ///     Returns a list of all orders.
        /// </summary>
        /// <returns>A list of orders</returns>
        public List<CargoOrderData> GetOrderList()
        {
            var list = new List<CargoOrderData>();

            foreach (var order in _orders)
            {
                list.Add(order);
            }

            return list;
        }
    }
}
