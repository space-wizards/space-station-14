using Robust.Shared.Serialization;
using Content.Shared.Access.Components;
namespace Content.Shared.Cargo
{
    [NetSerializable, Serializable]
    public sealed class CargoOrderData
    {
        public int OrderNumber;
        public string ProductId;
        public int Amount;
        public string Requester;
        // public String RequesterRank; // TODO Figure out how to get Character ID card data
        // public int RequesterId;
        public string Reason;
        public bool Approved;
        public string Approver = string.Empty;
        public string ApproverName = string.Empty;
        public string ApproverJob = string.Empty;

        public CargoOrderData(int orderNumber, string productId, int amount, string requester, string reason)
        {
            OrderNumber = orderNumber;
            Requester = requester;
            Reason = reason;
            ProductId = productId;
            Amount = amount;
        }

        public void setApproverData(IdCardComponent? idCard) {
            ApproverName = idCard?.FullName ?? string.Empty;
            ApproverJob = idCard?.JobTitle ?? string.Empty;
        }
    }
}
