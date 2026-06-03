using System.Text;
using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Cargo
{
    [DataDefinition, NetSerializable, Serializable]
    public sealed partial class CargoOrderData
    {
        /// <summary>
        /// A unique ID which identifies this order.
        /// Counts up from 1 with each order placed
        /// </summary>
        [DataField]
        public int OrderId { get; private set; }

        /// <summary>
        /// The ID of the cargo product ordered.
        /// </summary>
        [DataField]
        public ProtoId<CargoProductPrototype> Product { get; private set; }

        /// <summary>
        /// The number of items in the order. Not readonly, as it might change
        /// due to caps on the amount of orders that can be placed.
        /// </summary>
        [DataField]
        public int OrderQuantity { get; set; }

        /// <summary>
        /// How many instances of this order that we've already dispatched
        /// </summary>
        [DataField]
        public int NumDispatched { get; set; } = 0;

        /// <summary>
        /// Requester field on the order menu
        /// Defaults to the name of the orderer
        /// </summary>
        [DataField]
        public string Requester { get; private set; }

        // public String RequesterRank; // TODO Figure out how to get Character ID card data
        // public int RequesterId;
        [DataField]
        public string Reason { get; private set; }

        /// <summary>
        /// Wether this order has been approved for delivery
        /// </summary>
        public bool Approved { get; set; }

        /// <summary>
        /// If this order has been assigned to be delivered differently from the
        /// </summary>
        [ViewVariables]
        public bool Assigned { get; set; }

        /// <summary>
        /// The entity assigned to deliver, only not null if not the ATS
        /// </summary>
        [ViewVariables]
        public NetEntity? AssignedEntity { get; set; }

        /// <summary>
        /// The console which approved the order, used for telepad linking
        /// </summary>
        [ViewVariables]
        public NetEntity? ApprovingConsole { get; set; }

        /// <summary>
        /// The ID of the person who approved the order
        /// </summary>
        [DataField]
        public string? Approver { get; set; }

        /// <summary>
        /// Which account to deduct funds from when ordering
        /// </summary>
        [DataField]
        public ProtoId<CargoAccountPrototype> Account { get; private set; }

        /// <summary>
        /// If the order should be visible in any UI which displays orders
        /// </summary>
        [DataField]
        public bool Visible { get; set; } = true;

        public CargoOrderData(
            int orderId,
            ProtoId<CargoProductPrototype> product,
            int amount,
            string requester,
            string reason,
            ProtoId<CargoAccountPrototype> account
        )
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
