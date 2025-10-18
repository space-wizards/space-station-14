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
public sealed class PowerChargeState : BoundUserInterfaceState
{
    public bool On;
    // 0 -> 255
    public byte Charge;
    public PowerChargePowerStatus PowerStatus;
    public short PowerDraw;
    public short PowerDrawMax;
    public short EtaSeconds;

    public PowerChargeState(
        bool on,
        byte charge,
        PowerChargePowerStatus powerStatus,
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
public enum PowerChargeUiKey
{
    Key
}

[Serializable, NetSerializable]
public enum PowerChargeVisuals
{
    State,
    Charge,
    Active
}

[Serializable, NetSerializable]
public enum PowerChargeStatus
{
    Broken,
    Unpowered,
    Off,
    On
}

[Serializable, NetSerializable]
public enum PowerChargePowerStatus : byte
{
    Off,
    Discharging,
    Charging,
    FullyCharged
}
