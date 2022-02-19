using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Access.Systems;
using Content.Shared.Cargo;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.Cargo
{
    public sealed class CargoOrderDatabase
    {
        private readonly Dictionary<int, CargoOrderData> _orders = new();
        private int _orderNumber = 0;

        public CargoOrderDatabase(int id)
        {
            Id = id;
            CurrentOrderSize = 0;
            MaxOrderSize = 20;
        }

        public int Id { get; private set; }
        public int CurrentOrderSize { get; private set; }
        public int MaxOrderSize { get; private set; }

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

        public bool TryGetOrder(int id, [NotNullWhen(true)] out CargoOrderData? order)
        {
            return _orders.TryGetValue(id, out order);
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
        public bool ApproveOrder(string approver, int orderNumber)
        {
            if (CurrentOrderSize == MaxOrderSize ||
                !_orders.TryGetValue(orderNumber, out var order) ||
                order.Approved)
            {
                return false;
            }
            else if (CurrentOrderSize + order.Amount > MaxOrderSize)
            {
                AddOrder(
                    order.Requester,
                    Loc.GetString("cargo-order-database-order-overflow-message", ("placeholder", order.Reason.Replace(" (Overflow)", string.Empty))),
                    order.ProductId,
                    order.Amount - MaxOrderSize - CurrentOrderSize, order.PayingAccountId);

                order.Amount = MaxOrderSize - CurrentOrderSize;
            }

            order.Approved = true;
            order.Approver = approver;

            CurrentOrderSize += order.Amount;
            return true;
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

        /// <summary>
        ///     Clears the current order capacity. This allows more orders to be processed and is invoked after an order is dispatched.
        /// </summary>
        public void ClearOrderCapacity()
        {
            CurrentOrderSize = 0;
        }
    }
}
