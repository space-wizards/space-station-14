using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Cargo.Components
{
    [NetworkedComponent()]
    public abstract class SharedCargoOrderDatabaseComponent : Component
    {
    }

    [NetSerializable, Serializable]
    public sealed class CargoOrderDatabaseState : ComponentState
    {
        public readonly List<CargoOrderData>? Orders;

        public CargoOrderDatabaseState(List<CargoOrderData>? orders)
        {
            Orders = orders;
        }
    }
}
