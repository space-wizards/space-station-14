using Robust.Shared.Serialization;

namespace Content.Shared.Cargo.Orders
{
    [NetSerializable, Serializable]
    public sealed class CargoOrderDataProduct : CargoOrderData
    {
        /// <summary>
        /// Prototype Id for the item to be created
        /// </summary>
        public readonly string ProductId;

        public CargoOrderDataProduct(int orderId, string productId, int price, int amount, string requester, string reason) : base(orderId, price, amount, requester, reason)
        {
            ProductId = productId;
        }

        public override CargoOrderStringRepresentation ToPrettyString(EntityManager entityManager)
        {
            return new CargoOrderStringRepresentation(OrderId, ProductId, null, Price, OrderQuantity, Requester,
                Reason, Approver);
        }

        public override CargoOrderData GetReducedOrder(int amount)
        {
            var order = new CargoOrderDataProduct(OrderId, ProductId, Price, amount, Requester, Reason)
            {
                Approver = Approver
            };
            return order;
        }
    }
}
