using System;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry
{
    [Serializable, NetSerializable]
    public class TransferAmountBoundInterfaceState : BoundUserInterfaceState
    {
        public FixedPoint2 Max;
        public FixedPoint2 Min;

        public TransferAmountBoundInterfaceState(FixedPoint2 max, FixedPoint2 min)
        {
            Max = max;
            Min = min;
        }
    }

    [Serializable, NetSerializable]
    public class TransferAmountSetValueMessage : BoundUserInterfaceMessage
    {
        public FixedPoint2 Value;

        public TransferAmountSetValueMessage(FixedPoint2 value)
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
