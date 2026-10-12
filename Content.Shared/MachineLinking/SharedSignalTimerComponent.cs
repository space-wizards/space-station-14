using Robust.Shared.Serialization;

namespace Content.Shared.MachineLinking;

/// <summary>
/// Bound user interface key for the signal timer.
/// </summary>
[Serializable, NetSerializable]
public enum SignalTimerUiKey : byte
{
    Key,
}

/// <summary>
/// Message requesting a change to the signal timer's displayed text.
/// </summary>
[Serializable, NetSerializable]
public sealed class SignalTimerTextChangedMessage(string text) : BoundUserInterfaceMessage
{
    public readonly string Text = text;
}

/// <summary>
/// Message requesting a change to the signal timer's delay.
/// </summary>
[Serializable, NetSerializable]
public sealed class SignalTimerDelayChangedMessage(TimeSpan delay) : BoundUserInterfaceMessage
{
    public readonly TimeSpan Delay = delay;
}

/// <summary>
/// Message requesting that the signal timer start counting down.
/// </summary>
[Serializable, NetSerializable]
public sealed class SignalTimerStartMessage : BoundUserInterfaceMessage;
