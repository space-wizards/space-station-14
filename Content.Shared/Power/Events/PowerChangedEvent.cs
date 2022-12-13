namespace Content.Shared.Power.Events;

/// <summary>
/// Raised whenever an ApcPowerReceiver becomes powered / unpowered.
/// Does nothing on the client.
/// </summary>
[ByRefEvent]
public readonly record struct PowerChangedEvent(bool Powered, float ReceivingPower)
{
    public readonly bool Powered = Powered;
    public readonly float ReceivingPower = ReceivingPower;
}
