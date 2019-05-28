using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;

namespace Content.Shared.GameObjects.Components.Power
{
    public abstract class SharedSmesComponent : Component
    {
        public sealed override string Name => "Smes";
    }

    [Serializable, NetSerializable]
    public enum SmesVisuals
    {
        LastChargeState,
        LastChargeLevel,
    }

    [Serializable, NetSerializable]
    public enum SmesChargeState
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
    public sealed class SmesBoundInterfaceState : BoundUserInterfaceState
    {
        public readonly bool MainBreaker;
        public readonly SmesExternalPowerState SmesExternalPower;
        public readonly float Charge;
        public readonly float ChargeRate;
        public readonly float DistributionRate;

        public SmesBoundInterfaceState(bool mainBreaker, SmesExternalPowerState smesExternalPower, float charge, float chargeRate, float distributionRate)
        {
            MainBreaker = mainBreaker;
            SmesExternalPower = smesExternalPower;
            Charge = charge;
            ChargeRate = chargeRate;
            DistributionRate = distributionRate;
        }
    }

    [Serializable, NetSerializable]
    public sealed class SmesToggleMainBreakerMessage : BoundUserInterfaceMessage
    {
    }

    public enum SmesExternalPowerState
    {
        None,
        Low,
        Good,
    }

    [NetSerializable, Serializable]
    public enum SmesUiKey
    {
        Key,
    }
}
