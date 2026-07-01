namespace Content.Shared.Power.Events;

/// <summary>
/// Raised whenever an PowerReceiver becomes powered / unpowered.
/// </summary>
[ByRefEvent]
public readonly record struct PowerChangedEvent(bool Powered, float ReceivingPower);
