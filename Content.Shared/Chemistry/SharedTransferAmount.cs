using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry
{
    /// <summary>
    /// Send by the client when setting the transfer amount using the BUI.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class TransferAmountSetValueMessage(FixedPoint2 value) : BoundUserInterfaceMessage
    {
        /// <summary>
        /// The new transfer amount.
        /// </summary>
        public FixedPoint2 Value = value;
    }

    [Serializable, NetSerializable]
    public enum TransferAmountUiKey
    {
        Key,
    }
}
