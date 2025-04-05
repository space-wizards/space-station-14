using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.DeviceLinking
{
    [Serializable, NetSerializable]
    public sealed class ThresholdAmountBoundInterfaceState : BoundUserInterfaceState
    {
        public FixedPoint2 Max;
        public FixedPoint2 Min;

        public ThresholdAmountBoundInterfaceState(FixedPoint2 max, FixedPoint2 min)
        {
            Max = max;
            Min = min;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ThresholdAmountSetValueMessage : BoundUserInterfaceMessage
    {
        public FixedPoint2 Value;

        public ThresholdAmountSetValueMessage(FixedPoint2 value)
        {
            Value = value;
        }
    }

    [Serializable, NetSerializable]
    public enum ThresholdAmountUiKey
    {
        Key,
    }
}
