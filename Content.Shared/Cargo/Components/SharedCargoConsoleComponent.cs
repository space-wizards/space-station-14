using System;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Cargo.Components
{
    public class SharedCargoConsoleComponent : Component
    {
        [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;

        /// <summary>
        ///    Sends away or requests shuttle
        /// </summary>
        [Serializable, NetSerializable]
        public class CargoConsoleShuttleMessage : BoundUserInterfaceMessage
        {
            public CargoConsoleShuttleMessage()
            {
            }
        }

        /// <summary>
        ///     Add order to database.
        /// </summary>
        [Serializable, NetSerializable]
        public class CargoConsoleAddOrderMessage : BoundUserInterfaceMessage
        {
            public string Requester;
            public string Reason;
            public string ProductId;
            public int Amount;

            public CargoConsoleAddOrderMessage(string requester, string reason, string productId, int amount)
            {
                Requester = requester;
                Reason = reason;
                ProductId = productId;
                Amount = amount;
            }
        }

        /// <summary>
        ///     Remove order from database.
        /// </summary>
        [Serializable, NetSerializable]
        public class CargoConsoleRemoveOrderMessage : BoundUserInterfaceMessage
        {
            public int OrderNumber;

            public CargoConsoleRemoveOrderMessage(int orderNumber)
            {
                OrderNumber = orderNumber;
            }
        }

        /// <summary>
        ///     Set order in database as approved.
        /// </summary>
        [Serializable, NetSerializable]
        public class CargoConsoleApproveOrderMessage : BoundUserInterfaceMessage
        {
            public int OrderNumber;

            public CargoConsoleApproveOrderMessage(int orderNumber)
            {
                OrderNumber = orderNumber;
            }
        }

        [NetSerializable, Serializable]
        public enum CargoConsoleUiKey
        {
            Key
        }
    }

    [NetSerializable, Serializable]
    public class CargoConsoleInterfaceState : BoundUserInterfaceState
    {
        public readonly bool RequestOnly;
        public readonly int BankId;
        public readonly string BankName;
        public readonly int BankBalance;
        public readonly (int CurrentCapacity, int MaxCapacity) ShuttleCapacity;

        public CargoConsoleInterfaceState(bool requestOnly, int bankId, string bankName, int bankBalance, (int CurrentCapacity, int MaxCapacity) shuttleCapacity)
        {
            RequestOnly = requestOnly;
            BankId = bankId;
            BankName = bankName;
            BankBalance = bankBalance;
            ShuttleCapacity = shuttleCapacity;
        }
    }
}
