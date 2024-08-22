namespace Content.Shared.Power.Components;

/// <summary>
/// Raised whenever an ApcPowerReceiver becomes powered / unpowered.
/// Does nothing on the client.
/// </summary>
[ByRefEvent]
public readonly record struct PowerChangedEvent(bool Powered, float ReceivingPower);