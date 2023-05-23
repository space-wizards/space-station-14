using Robust.Shared.Serialization;
using Content.Shared.Access.Components;
using System.Text;
namespace Content.Shared.Cargo
{
    [NetSerializable, Serializable]
    public sealed class CargoOrderData
    {
        /// <summary>
        /// A unique (arbitrary) ID which identifies this order.
        /// </summary>
        public readonly int OrderId;

        /// <summary>
        /// Prototype id for the item to create
        /// </summary>
        public readonly string ProductId;

        /// <summary>
        /// The number of items in the order. Not readonly, as it might change
        /// due to caps on the amount of orders that can be placed.
        /// </summary>
        public int OrderQuantity;

        /// <summary>
        /// How many instances of this order that we've already dispatched
        /// <summary>
        public int NumDispatched = 0;

        public readonly string Requester;
        // public String RequesterRank; // TODO Figure out how to get Character ID card data
        // public int RequesterId;
        public readonly string Reason;
        public  bool Approved => Approver is not null;
        public string? Approver;

        public CargoOrderData(int orderId, string productId, int amount, string requester, string reason)
        {
            OrderId = orderId;
            ProductId = productId;
            OrderQuantity = amount;
            Requester = requester;
            Reason = reason;
        }

        public void SetApproverData(string? fullname, string? jobtitle)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(fullname))
            {
                sb.Append($"{fullname} ");
            }
            if (!string.IsNullOrWhiteSpace(jobtitle))
            {
                sb.Append($"({jobtitle})");
            }
            Approver = sb.ToString();
        }

        public void SetApproverData(IdCardComponent? idCard)
        {
            SetApproverData(idCard?.FullName, idCard?.JobTitle);
        }
    }
}
