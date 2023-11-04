using Robust.Shared.Serialization;
using Content.Shared.Access.Components;
using System.Text;
namespace Content.Shared.Cargo
{
    [NetSerializable, Serializable]
    public sealed class CargoOrderData
    {
        /// <summary>
        /// Price when the order was added.
        /// </summary>
        public int Price;

        /// <summary>
        /// A unique (arbitrary) ID which identifies this order.
        /// </summary>
        public readonly int OrderId;

        /// <summary>
        /// Prototype Id for the item to be created
        /// </summary>
        public readonly string ProductId;

        /// <summary>
        /// The number of items in the order. Not readonly, as it might change
        /// due to caps on the amount of orders that can be placed.
        /// </summary>
        public int OrderQuantity;

        /// <summary>
        /// How many instances of this order that we've already dispatched
        /// </summary>
        public int NumDispatched = 0;

        public readonly string Requester;
        // public String RequesterRank; // TODO Figure out how to get Character ID card data
        // public int RequesterId;
        public readonly string Reason;
        public  bool Approved => Approver is not null;
        public string? Approver;

        public CargoOrderData(int orderId, string productId, int price, int amount, string requester, string reason)
        {
            OrderId = orderId;
            ProductId = productId;
            Price = price;
            OrderQuantity = amount;
            Requester = requester;
            Reason = reason;
        }

        public void SetApproverData(string? fullName, string? jobTitle)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                sb.Append($"{fullName} ");
            }
            if (!string.IsNullOrWhiteSpace(jobTitle))
            {
                sb.Append($"({jobTitle})");
            }
            Approver = sb.ToString();
        }
    }
}
