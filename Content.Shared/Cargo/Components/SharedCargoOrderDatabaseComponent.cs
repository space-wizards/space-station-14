#nullable enable
using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Cargo.Components
{
    [NetworkedComponent()]
    public class SharedCargoOrderDatabaseComponent : Component
    {
        public sealed override string Name => "CargoOrderDatabase";
    }

    [NetSerializable, Serializable]
    public class CargoOrderDatabaseState : ComponentState
    {
        public readonly List<CargoOrderData>? Orders;

        public CargoOrderDatabaseState(List<CargoOrderData>? orders)
        {
            Orders = orders;
        }
    }
}
