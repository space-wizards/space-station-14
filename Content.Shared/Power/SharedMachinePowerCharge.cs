using Robust.Shared.Serialization;

namespace Content.Shared.Power;

/// <summary>
///     Sent to the server to set whether the machine should be on or off
/// </summary>
[Serializable, NetSerializable]
public sealed class SwitchChargingMachineMessage : BoundUserInterfaceMessage
{
    public bool On;

    public SwitchChargingMachineMessage(bool on)
    {
        On = on;
    }
}

[Serializable, NetSerializable]
public sealed class ChargingMachineState : BoundUserInterfaceState
{
    public bool On;
    // 0 -> 255
    public byte Charge;
    public MachinePowerChargePowerStatus PowerStatus;
    public short PowerDraw;
    public short PowerDrawMax;
    public short EtaSeconds;

    public ChargingMachineState(
        bool on,
        byte charge,
        MachinePowerChargePowerStatus powerStatus,
        short powerDraw,
        short powerDrawMax,
        short etaSeconds)
    {
        On = on;
        Charge = charge;
        PowerStatus = powerStatus;
        PowerDraw = powerDraw;
        PowerDrawMax = powerDrawMax;
        EtaSeconds = etaSeconds;
    }
}

[Serializable, NetSerializable]
public enum MachinePowerChargeUiKey
{
    Key
}

[Serializable, NetSerializable]
public enum MachinePowerChargeVisuals
{
    State,
    Charge
}

[Serializable, NetSerializable]
public enum MachinePowerChargeStatus
{
    Broken,
    Unpowered,
    Off,
    On
}

[Serializable, NetSerializable]
public enum MachinePowerChargePowerStatus : byte
{
    Off,
    Discharging,
    Charging,
    FullyCharged
}
