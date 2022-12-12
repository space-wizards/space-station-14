using Robust.Shared.Serialization;

namespace Content.Shared.Power.Events;

/// <summary>
/// Raised whenever an ApcPowerReceiver becomes powered / unpowered.
/// </summary>
[ByRefEvent]
[Serializable, NetSerializable]
public readonly record struct PowerChangedEvent(bool Powered, float ReceivingPower)
{
    public readonly bool Powered = Powered;
    public readonly float ReceivingPower = ReceivingPower;
}
