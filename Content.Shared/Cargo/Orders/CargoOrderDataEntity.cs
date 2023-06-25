using Robust.Shared.Serialization;

namespace Content.Shared.Cargo.Orders
{
    [NetSerializable, Serializable]
    public abstract class CargoOrderDataEntity : CargoOrderData
    {
        /// <summary>
        /// Entity to use as the order
        /// </summary>
        public readonly EntityUid? OrderEntity;

        public CargoOrderDataEntity(int orderId, EntityUid orderEntityUid, int price, string requester, string reason) : base(orderId, price, 1, requester, reason)
        {
            OrderEntity = orderEntityUid;
        }
    }
}
