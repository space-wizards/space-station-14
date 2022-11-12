using Robust.Shared.Serialization;

namespace Content.Shared.APC
{
    [Serializable, NetSerializable]
    public enum ApcVisuals
    {
        /// <summary>
        ///     APC lights/HUD.
        /// </summary>
        ChargeState,

        /// <summary>
        ///     APC frame.
        /// </summary>
        PanelState
    }

    [Serializable, NetSerializable]
    public enum ApcPanelState
    {
        /// <summary>
        ///     APC is closed.
        /// </summary>
        Closed,
        /// <summary>
        ///     APC opened.
        /// </summary>
        Open
    }

    [Serializable, NetSerializable]
    public enum ApcChargeState
    {
        /// <summary>
        ///     APC does not have enough power to charge cell (if necessary) and keep powering the area.
        /// </summary>
        Lack,

        /// <summary>
        ///     APC is not full but has enough power.
        /// </summary>
        Charging,

        /// <summary>
        ///     APC battery is full and has enough power.
        /// </summary>
        Full,

        /// <summary>
        /// APC is emagged (and not displaying other useful power colors at a glance)
        /// </summary>
        Emag,
    }

    [Serializable, NetSerializable]
    public sealed class ApcBoundInterfaceState : BoundUserInterfaceState
    {
        public readonly bool MainBreaker;
        public readonly ApcExternalPowerState ApcExternalPower;
        public readonly float Charge;

        public ApcBoundInterfaceState(bool mainBreaker, ApcExternalPowerState apcExternalPower, float charge)
        {
            MainBreaker = mainBreaker;
            ApcExternalPower = apcExternalPower;
            Charge = charge;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ApcToggleMainBreakerMessage : BoundUserInterfaceMessage
    {
    }

    public enum ApcExternalPowerState
    {
        None,
        Low,
        Good,
    }

    [NetSerializable, Serializable]
    public enum ApcUiKey
    {
        Key,
    }
}
