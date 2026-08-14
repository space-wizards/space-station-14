using Robust.Shared.Serialization;

namespace Content.Shared.APC;

/// <summary>
/// AppearanceData keys for APC data.
/// </summary>
[Serializable, NetSerializable]
public enum ApcVisuals : byte
{
    /// <summary>
    /// APC channel state.
    /// Contains an <see cref="ApcChannelState"/>.
    /// </summary>
    ChannelState,

    /// <summary>
    /// APC lights/HUD.
    /// Contains an <see cref="ApcChargeState"/>.
    /// </summary>
    ChargeState,
}

/// <summary>
/// Sprite layers for APC visuals.
/// </summary>
public enum ApcVisualLayers : byte
{
    /// <summary>
    /// Layer used for the status light for the channel.
    /// "Is power going out of the device? Is the breaker tripped?"
    /// </summary>
    Equipment,

    /// <summary>
    /// The sprite layer used for the APC screen overlay.
    /// "How much energy is contained in the device?"
    /// </summary>
    ChargeState,
}

/// <summary>
/// APC power channel states.
/// Is the APC delivering power currently?
/// </summary>
/// <seealso cref="ApcVisuals.ChannelState"/>
[Serializable, NetSerializable]
public enum ApcChannelState : byte
{
    /// <summary>
    /// The APC is operating normally, and is currently not delivering power.
    /// </summary>
    Off = 0,

    /// <summary>
    /// The APC is operating normally, and is delivering power to the network.
    /// </summary>
    On = 1,

    /// <summary>
    /// The APC's breaker has been opened manually, and cannot deliver power.
    /// </summary>
    BreakerOpen = 2,

    /// <summary>
    /// The APC's breaker has been tripped, and cannot deliver power.
    /// </summary>
    BreakerTripped = 3,

    /// <summary>
    /// The total number of states to show.
    /// </summary>
    NumStates = 4,
}

/// <summary>
/// APC charge states.
/// How much energy is the APC holding?
/// </summary>
/// <seealso cref="ApcVisuals.ChannelState"/>
[Serializable, NetSerializable]
public enum ApcChargeState : byte
{
    /// <summary>
    /// APC does not have enough power to charge cell (if necessary) and keep powering the area.
    /// </summary>
    Lack = 0,

    /// <summary>
    /// APC is not full but has enough power.
    /// </summary>
    Charging = 1,

    /// <summary>
    /// APC battery is full and has enough power.
    /// </summary>
    Full = 2,

    /// <summary>
    /// APC is being remotely accessed.
    /// Currently unimplemented, though the corresponding sprite state exists in the RSI.
    /// </summary>
    Remote = 3,

    /// <summary>
    /// The APC's breaker has been tripped.
    /// </summary>
    Tripped = 4,

    /// <summary>
    /// The number of valid states charge states the APC can be in.
    /// </summary>
    NumStates = 5,

    /// <summary>
    /// APC is emagged (and not displaying other useful power colors at a glance)
    /// </summary>
    Emag = byte.MaxValue,
}

/// <summary>
/// The state of an APC BUI.
/// Contains the details of the charge, breaker, and power delivery for a given APC.
/// </summary>
[Serializable, NetSerializable]
public sealed class ApcBoundInterfaceState(bool mainBreaker,
    int power,
    ApcExternalPowerState apcExternalPower,
    float charge,
    float maxLoad,
    bool tripped) : BoundUserInterfaceState, IEquatable<ApcBoundInterfaceState>
{
    /// <summary>
    /// If true, the breaker is active, and the APC can deliver power.
    /// </summary>
    public readonly bool MainBreaker = mainBreaker;

    /// <summary>
    /// The current amount of power being delivered, in Watts.
    /// </summary>
    public readonly int Power = power;

    /// <summary>
    /// The approximate amount of energy contained within the APC.
    /// </summary>
    public readonly ApcExternalPowerState ApcExternalPower = apcExternalPower;

    /// <summary>
    /// The energy remaining in the APC, in Joules
    /// </summary>
    public readonly float Charge = charge;

    /// <summary>
    /// The maximum allowable load of the APC, in Watts.
    /// </summary>
    public readonly float MaxLoad = maxLoad;

    /// <summary>
    /// Whether or not the breaker has been tripped.
    /// </summary>
    public readonly bool Tripped = tripped;

    /// <inheritdoc/>
    public bool Equals(ApcBoundInterfaceState? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return MainBreaker == other.MainBreaker &&
                Power == other.Power &&
                ApcExternalPower == other.ApcExternalPower &&
                MathHelper.CloseTo(Charge, other.Charge) &&
                MathHelper.CloseTo(MaxLoad, other.MaxLoad) &&
                Tripped == other.Tripped;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is ApcBoundInterfaceState other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(MainBreaker, Power, (int)ApcExternalPower, Charge, MaxLoad, Tripped);
    }
}

/// <summary>
/// A request to toggle the main breaker of an APC.
/// </summary>
[Serializable, NetSerializable]
public sealed class ApcToggleMainBreakerMessage : BoundUserInterfaceMessage;

/// <summary>
/// The amount of energy stored on the APC.
/// Used as a traffic light state in BUI states.
/// </summary>
public enum ApcExternalPowerState : byte
{
    None,
    Low,
    Good,
}

/// <summary>
/// UI keys for the APC.
/// </summary>
[NetSerializable, Serializable]
public enum ApcUiKey : byte
{
    Key,
}

