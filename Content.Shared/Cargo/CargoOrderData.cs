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
        /// List of items included in this order.
        /// </summary>
        [DataField]
        public List<CargoOrderItemData> Basket;


        /// <summary>
        /// A string representation of the requestor's name.
        /// </summary>
        [DataField]
        public string Requester { get; private set; }

        // public String RequesterRank; // TODO Figure out how to get Character ID card data
        // public int RequesterId;

        /// <summary>
        /// A player-provided string of the reason for the order.
        /// </summary>
        [DataField]
        public string Reason { get; private set; }

        /// <summary>
        /// If the order has been approved.
        /// </summary>
        public bool Approved;

        /// <summary>
        /// The console that approved the order.
        /// </summary>
        [DataField]
        public NetEntity? ApprovingConsole { get; set; }

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
        /// A string representation of the approver's name, unless the ordering console was emagged.
        /// </summary>
        [DataField]
        public string? Approver { get; set; }

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
        /// Which account to deduct funds from when ordering.
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
            List<CargoOrderItemData> basket,
            string requester,
            string reason,
            ProtoId<CargoAccountPrototype> account
        )
        {
            OrderId = orderId;
            Basket = basket;
            Requester = requester;
            Reason = reason;
            Account = account;
        }

        /// <summary>
        /// Sets the approver for this order.
        /// </summary>
        /// <param name="approver">Nullable string representation of the approver's name.</param>
        public void SetApproverData(string? approver)
        {
            Approver = approver;
        }

        /// <summary>
        /// Sets the approver for this order by concatenating the name and job title.
        /// If both are null, will be an empty string.
        /// </summary>
        /// <param name="fullName">Nullable string representation of the approver's full name.</param>
        /// <param name="jobTitle">Nullable string representation of the approver's job title.</param>
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
