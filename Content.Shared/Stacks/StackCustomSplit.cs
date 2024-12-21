using Robust.Shared.Serialization;

namespace Content.Shared.Stacks
{
    [Serializable, NetSerializable]
    public sealed class StackCustomSplitAmountMessage : BoundUserInterfaceMessage
    {
        public int Amount;

        public StackCustomSplitAmountMessage(int amount)
        {
            Amount = amount;
        }
    }

    [Serializable, NetSerializable]
    public enum StackCustomSplitUiKey
    {
        Key,
    }
}
