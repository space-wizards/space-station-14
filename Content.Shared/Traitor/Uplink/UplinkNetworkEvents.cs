using Robust.Shared.Serialization;

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
