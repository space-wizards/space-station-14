using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;

namespace Content.Shared.Traitor.Uplink
{
    [Serializable, NetSerializable]
    public sealed class UplinkBuyListingMessage : BoundUserInterfaceMessage
    {
        public string ItemId;

        public UplinkBuyListingMessage(string itemId)
        {
            ItemId = itemId;
        }
    }

    [Serializable, NetSerializable]
    public sealed class UplinkRequestUpdateInterfaceMessage : BoundUserInterfaceMessage
    {
        public UplinkRequestUpdateInterfaceMessage()
        {

        }
    }
}
