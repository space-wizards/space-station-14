using Robust.Shared.Serialization;

namespace Content.Shared.Cargo.Orders
{
    [NetSerializable, Serializable]
    public sealed class CargoOrderDataEntity : CargoOrderData
    {
        /// <summary>
        /// Entity to use as the order
        /// </summary>
        public readonly EntityUid OrderEntity;

        public CargoOrderDataEntity(int orderId, EntityUid orderEntityUid, int price, string requester, string reason) : base(orderId, price, 1, requester, reason)
        {
            OrderEntity = orderEntityUid;
        }

        public override CargoOrderStringRepresentation ToPrettyString(EntityManager entityManager)
        {
            return new CargoOrderStringRepresentation(OrderId, entityManager.ToPrettyString(OrderEntity), OrderEntity, Price, OrderQuantity, Requester,
                Reason, Approver);
        }

        public override CargoOrderData GetReducedOrder(int amount)
        {
            throw new NotImplementedException(); // The amount is always 1, a reduced order is always an order for nothing. Nothing should ever try to get a reduced amount of an order of 1
        }
    }
}
