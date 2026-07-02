using Robust.Shared.Serialization;

namespace Content.Shared.APC
{
    [Serializable, NetSerializable]
    public enum ApcVisuals : byte
    {
        /// <summary>
        /// APC locks.
        /// </summary>
        LockState,
        /// <summary>
        /// APC channels.
        /// </summary>
        ChannelState,
        /// <summary>
        /// APC lights/HUD.
        /// </summary>
        ChargeState,
    }

    [Serializable, NetSerializable]
    public enum ApcPanelState : sbyte
    {
        /// <summary>
        /// APC is closed.
        /// </summary>
        Closed = 0,
        /// <summary>
        /// APC is opened.
        /// </summary>
        Open = 1,
        /// <summary>
        /// APC is oaisdoj.
        /// </summary>
        Error = -1,
    }

    /// <summary>
    /// The state of the APC interface lock.
    /// None of this is implemented.
    /// </summary>
    [Serializable, NetSerializable]
    public enum ApcLockState : sbyte
    {
        /// <summary>
        /// Empty bitmask.
        /// </summary>
        None = 0,

        /// <summary>
        /// Bitfield indicating status of APC lock indicator.
        /// </summary>
        Lock = (1<<0),
        /// <summary>
        /// Bit state indicating that the given APC lock is unlocked.
        /// </summary>
        Unlocked = None,
        /// <summary>
        /// Bit state indicating that the given APC lock is locked.
        /// </summary>
        Locked = (1<<0),

        /// <summary>
        /// Bitmask for the full state for a given APC lock indicator.
        /// </summary>
        All = (Lock),

        /// <summary>
        /// The log 2 width in bits of the bitfields indicating the status of an APC lock indicator.
        /// Used for bit shifting operations (Mask for the state for indicator i is (All << (i << LogWidth))).
        /// </summary>
        LogWidth = 0,
    }

    /// <summary>
    /// APC power channel states.
    /// </summary>
    [Serializable, NetSerializable]
    public enum ApcChannelState : sbyte
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

    [Serializable, NetSerializable]
    public enum ApcChargeState : sbyte
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
        Emag = -1,
    }

    [Serializable, NetSerializable]
    public sealed class ApcBoundInterfaceState : BoundUserInterfaceState, IEquatable<ApcBoundInterfaceState>
    {
        public readonly bool MainBreaker;
        public readonly int Power;
        public readonly ApcExternalPowerState ApcExternalPower;
        public readonly float Charge;
        public readonly float MaxLoad;
        public readonly bool Tripped;

        public ApcBoundInterfaceState(bool mainBreaker, int power, ApcExternalPowerState apcExternalPower, float charge, float maxLoad, bool tripped)
        {
            MainBreaker = mainBreaker;
            Power = power;
            ApcExternalPower = apcExternalPower;
            Charge = charge;
            MaxLoad = maxLoad;
            Tripped = tripped;
        }

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

        public override bool Equals(object? obj)
        {
            return ReferenceEquals(this, obj) || obj is ApcBoundInterfaceState other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(MainBreaker, Power, (int) ApcExternalPower, Charge, MaxLoad, Tripped);
        }
    }

    [Serializable, NetSerializable]
    public sealed class ApcToggleMainBreakerMessage : BoundUserInterfaceMessage
    {
    }

    public enum ApcExternalPowerState : byte
    {
        None,
        Low,
        Good,
    }

    [NetSerializable, Serializable]
    public enum ApcUiKey : byte
    {
        Key,
    }
}
