using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;

namespace Content.Shared.Traitor.Uplink
{
    [Serializable, NetSerializable]
    public sealed class UplinkBuySuccessMessage : EntityEventArgs
    {
    }

    [Serializable, NetSerializable]
    public sealed class UplinkInsufficientFundsMessage : EntityEventArgs
    {
    }
}
