using System;
using Robust.Shared.Serialization;

namespace Content.Shared.Cargo
{
    [NetSerializable, Serializable]
    public sealed class CargoOrderData
    {
        public int OrderNumber;
        public string Requester;
        // public String RequesterRank; // TODO Figure out how to get Character ID card data
        // public int RequesterId;
        public string Reason;
        public string ProductId;
        public int Amount;
        public int PayingAccountId;
        public bool Approved;
        public string Approver = string.Empty;

        public CargoOrderData(int orderNumber, string requester, string reason, string productId, int amount, int payingAccountId)
        {
            OrderNumber = orderNumber;
            Requester = requester;
            Reason = reason;
            ProductId = productId;
            Amount = amount;
            PayingAccountId = payingAccountId;
        }
    }
}
