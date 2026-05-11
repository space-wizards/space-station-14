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
        /// List of items included in this order.
        /// </summary>
        [DataField]
        public List<CargoOrderItemData> Basket;

        [DataField]
        public string Requester { get; private set; }
        // public String RequesterRank; // TODO Figure out how to get Character ID card data
        // public int RequesterId;
        [DataField]
        public string Reason { get; private set; }
        [ViewVariables]
        public bool Approved;
        [ViewVariables]
        public bool Assigned;
        [ViewVariables]
        public NetEntity? AssignedEntity;
        [DataField]
        public string? Approver;

        /// <summary>
        /// Which account to deduct funds from when ordering
        /// </summary>
        [DataField]
        public ProtoId<CargoAccountPrototype> Account;

        public CargoOrderData(int orderId, List<CargoOrderItemData> basket, string requester, string reason, ProtoId<CargoAccountPrototype> account)
        {
            OrderId = orderId;
            Basket = basket;
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
