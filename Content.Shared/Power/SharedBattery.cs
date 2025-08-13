using Robust.Shared.Serialization;

namespace Content.Shared.Power;

/// <summary>
/// UI key for large battery (SMES/substation) UIs.
/// </summary>
[NetSerializable, Serializable]
public enum BatteryUiKey : byte
{
    Key,
}

/// <summary>
/// UI state for large battery (SMES/substation) UIs.
/// </summary>
/// <seealso cref="BatteryUiKey"/>
[Serializable, NetSerializable]
public sealed class BatteryBuiState : BoundUserInterfaceState
{
    // These are mostly just regular Pow3r parameters.

    // I/O
    public bool CanCharge;
    public bool CanDischarge;
    public bool SupplyingNetworkHasPower;
    public bool LoadingNetworkHasPower;
    public float CurrentReceiving;
    public float CurrentSupply;

    // Charge
    public float MaxChargeRate;
    public float MinMaxChargeRate;
    public float MaxMaxChargeRate;
    public float Efficiency;

    // Discharge
    public float MaxSupply;
    public float MinMaxSupply;
    public float MaxMaxSupply;

    // Storage
    public float Charge;
    public float Capacity;
}

/// <summary>
/// Sent client to server to change the input breaker state on a large battery.
/// </summary>
[Serializable, NetSerializable]
public sealed class BatterySetInputBreakerMessage(bool on) : BoundUserInterfaceMessage
{
    public bool On = on;
}

/// <summary>
/// Sent client to server to change the output breaker state on a large battery.
/// </summary>
[Serializable, NetSerializable]
public sealed class BatterySetOutputBreakerMessage(bool on) : BoundUserInterfaceMessage
{
    public bool On = on;
}

/// <summary>
/// Sent client to server to change the charge rate on a large battery.
/// </summary>
[Serializable, NetSerializable]
public sealed class BatterySetChargeRateMessage(float rate) : BoundUserInterfaceMessage
{
    public float Rate = rate;
}

/// <summary>
/// Sent client to server to change the discharge rate on a large battery.
/// </summary>
[Serializable, NetSerializable]
public sealed class BatterySetDischargeRateMessage(float rate) : BoundUserInterfaceMessage
{
    public float Rate = rate;
}

