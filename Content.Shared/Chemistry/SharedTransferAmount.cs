using System;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry
{
    [Serializable, NetSerializable]
    public class TransferAmountBoundInterfaceState : BoundUserInterfaceState
    {
        public ReagentUnit Max;
        public ReagentUnit Min;

        public TransferAmountBoundInterfaceState(ReagentUnit max, ReagentUnit min)
        {
            Max = max;
            Min = min;
        }
    }

    [Serializable, NetSerializable]
    public class TransferAmountSetValueMessage : BoundUserInterfaceMessage
    {
        public ReagentUnit Value;

        public TransferAmountSetValueMessage(ReagentUnit value)
        {
            Value = value;
        }
    }

    [Serializable, NetSerializable]
    public enum TransferAmountUiKey
    {
        Key,
    }
}
