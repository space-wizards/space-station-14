using System.Collections.Generic;
using Content.Server.GameObjects.Components.Cargo;
using Content.Shared.Prototypes.Cargo;

namespace Content.Server.Cargo
{
    public interface ICargoOrderDataManager
    {
        bool TryGetAccount(int id, out CargoOrderDatabase account);
        void AddOrder(int id, string requester, string reason, string productId, int amount, int payingAccountId);
        void RemoveOrder(int id, int orderNumber);
        void ApproveOrder(int id, int orderNumber);
        void AddComponent(CargoOrderDatabaseComponent component);
        List<CargoOrderData> GetOrdersFromAccount(int accountId);
        List<CargoOrderData> RemoveAndGetApprovedFrom(CargoOrderDatabase database);
        (int CurrentCapacity, int MaxCapacity) GetCapacity(int id);
    }
}
