using Robust.Shared.Serialization;

namespace Content.Shared.Cargo.Orders
{
    [NetSerializable, Serializable]
    public abstract class CargoOrderDataProduct : CargoOrderData
    {
        /// <summary>
        /// Prototype Id for the item to be created
        /// </summary>
        public readonly string ProductId;

        public CargoOrderDataProduct(int orderId, string productId, int price, int amount, string requester, string reason) : base(orderId, price, amount, requester, reason)
        {
            ProductId = productId;
        }
    }
}
