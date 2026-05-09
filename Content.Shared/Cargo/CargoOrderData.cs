using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System.Text;
namespace Content.Shared.Cargo
{
    [DataDefinition, NetSerializable, Serializable]
    public sealed partial class CargoOrderData
    {
        /// <summary>
        /// A unique (arbitrary) ID which identifies this order.
        /// </summary>
        [DataField]
        public int OrderId { get; private set; }

        /// <summary>
        /// The ID of the cargo product ordered.
        /// </summary>
        [DataField]
        public ProtoId<CargoProductPrototype> Product;

        /// <summary>
        /// The number of items in the order. Not readonly, as it might change
        /// due to caps on the amount of orders that can be placed.
        /// </summary>
        [DataField]
        public int OrderQuantity;

        /// <summary>
        /// How many instances of this order that we've already dispatched
        /// </summary>
        [DataField]
        public int NumDispatched = 0;

        [DataField]
        public string Requester { get; private set; }
        // public String RequesterRank; // TODO Figure out how to get Character ID card data
        // public int RequesterId;
        [DataField]
        public string Reason { get; private set; }
        public  bool Approved;
        [DataField]
        public string? Approver;

        /// <summary>
        /// Which account to deduct funds from when ordering
        /// </summary>
        [DataField]
        public ProtoId<CargoAccountPrototype> Account;

        public CargoOrderData(int orderId, ProtoId<CargoProductPrototype> product, int amount, string requester, string reason, ProtoId<CargoAccountPrototype> account)
        {
            OrderId = orderId;
            Product = product;
            OrderQuantity = amount;
            Requester = requester;
            Reason = reason;
            Account = account;
        }

        public void SetApproverData(string? approver)
        {
            Approver = approver;
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
