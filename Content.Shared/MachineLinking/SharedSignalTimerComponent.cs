using Robust.Shared.Serialization;

namespace Content.Shared.MachineLinking;

[Serializable, NetSerializable]
public enum SignalTimerUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class SignalTimerTextChangedMessage(string text) : BoundUserInterfaceMessage
{
    public readonly string Text = text;
}

[Serializable, NetSerializable]
public sealed class SignalTimerDelayChangedMessage(TimeSpan delay) : BoundUserInterfaceMessage
{
    public readonly TimeSpan Delay = delay;
}

[Serializable, NetSerializable]
public sealed class SignalTimerStartMessage : BoundUserInterfaceMessage;
