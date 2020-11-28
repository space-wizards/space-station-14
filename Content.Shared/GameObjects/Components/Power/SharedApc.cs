using System;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Power
{
    [Serializable, NetSerializable]
    public enum ApcVisuals
    {
        ChargeState
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
    }

    [Serializable, NetSerializable]
    public sealed class ApcBoundInterfaceState : BoundUserInterfaceState
    {
        public readonly bool MainBreaker;
        public readonly ApcExternalPowerState ApcExternalPower;
        public readonly float Charge;
        public readonly bool Disrupted;
        public readonly TimeSpan RemainingDisruption;
        public readonly bool DisruptionOnCoolDown;
        public readonly TimeSpan RemainingDisruptionCooldown;

        public ApcBoundInterfaceState(bool mainBreaker, ApcExternalPowerState apcExternalPower, float charge, bool disrupted, TimeSpan remainingDisruption, bool disruptionOnCoolDown, TimeSpan remainingDisruptionCooldown)
        {
            MainBreaker = mainBreaker;
            ApcExternalPower = apcExternalPower;
            Charge = charge;
            Disrupted = disrupted;
            RemainingDisruption = remainingDisruption;
            DisruptionOnCoolDown = disruptionOnCoolDown;
            RemainingDisruptionCooldown = remainingDisruptionCooldown;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ApcToggleMainBreakerMessage : BoundUserInterfaceMessage
    { 
    }

    [Serializable, NetSerializable]
    public sealed class ApcCyclePowerMessage : BoundUserInterfaceMessage
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
