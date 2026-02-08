using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.DeviceLinking;

[Serializable, NetSerializable]
public sealed class ThresholdAmountBoundInterfaceState : BoundUserInterfaceState
{
    public int Max;
    public int Min;

    public ThresholdAmountBoundInterfaceState(int max, int min)
    {
        Max = max;
        Min = min;
    }
}

[Serializable, NetSerializable]
public sealed class ThresholdAmountSetValueMessage : BoundUserInterfaceMessage
{
    public int Value;

    public ThresholdAmountSetValueMessage(int value)
    {
        Value = value;
    }
}

[Serializable, NetSerializable]
public enum ThresholdAmountUiKey
{
    Key,
}
