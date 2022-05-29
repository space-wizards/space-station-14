using System;
using System.Collections.Generic;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Robust.Shared.GameObjects;

namespace Content.Client.Cargo.Components
{
    [RegisterComponent]
    public sealed class CargoOrderDatabaseComponent : SharedCargoOrderDatabaseComponent
    {
        private readonly List<CargoOrderData> _orders = new();

        public IReadOnlyList<CargoOrderData> Orders => _orders;
        /// <summary>
        ///     Event called when the database is updated.
        /// </summary>
        public event Action? OnDatabaseUpdated;

        // TODO add account selector menu

        /// <summary>
        ///     Removes all orders from the database.
        /// </summary>
        public void Clear()
        {
            _orders.Clear();
        }

        /// <summary>
        ///     Adds an order to the database.
        /// </summary>
        /// <param name="order">The order to be added.</param>
        public void AddOrder(CargoOrderData order)
        {
            if (!_orders.Contains(order))
                _orders.Add(order);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);
            if (curState is not CargoOrderDatabaseState state)
                return;
            Clear();
            if (state.Orders == null)
                return;
            foreach (var order in state.Orders)
            {
                AddOrder(order);
            }

            OnDatabaseUpdated?.Invoke();
        }
    }
}
