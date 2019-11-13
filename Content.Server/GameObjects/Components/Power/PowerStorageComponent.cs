using System;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Power;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Power
{
    /// <summary>
    ///     Stores electrical energy. Used by power cells and SMESes.
    /// </summary>
    public abstract class PowerStorageComponent : Component, IExamine
    {
        [ViewVariables]
        public ChargeState LastChargeState { get; private set; } = ChargeState.Still;
        public DateTime LastChargeStateChange { get; private set; }

        /// <summary>
        ///     Maximum amount of energy the internal battery can store.
        ///     In Joules.
        /// </summary>
        [ViewVariables]
        public float Capacity => _capacity;
        private float _capacity = 10000; // Arbitrary value, replace.

        /// <summary>
        ///     Energy the battery is currently storing.
        ///     In Joules.
        ///     In most cases you should use <see cref="DeductCharge"/> and <see cref="AddCharge"/> to modify this.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public virtual float Charge
        {
            get => _charge;
            set => _charge = value;
        }

        private float _charge = 0;

        /// <summary>
        ///     Rate at which energy will be taken to charge internal battery.
        ///     In Watts.
        /// </summary>
        [ViewVariables]
        public float ChargeRate => _chargeRate;
        private float _chargeRate = 1000;

        /// <summary>
        ///     Rate at which energy will be distributed to the powernet if needed.
        ///     In Watts.
        /// </summary>
        [ViewVariables]
        public float DistributionRate => _distributionRate;
        private float _distributionRate = 1000;

        [ViewVariables]
        public bool Full => Charge >= Capacity;

        public event Action OnChargeChanged;
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _capacity, "capacity", 10000);
            serializer.DataField(ref _charge, "charge", 0);
            serializer.DataField(ref _chargeRate, "chargerate", 1000);
            serializer.DataField(ref _distributionRate, "distributionrate", 1000);
        }

        protected virtual void ChargeChanged()
        {
            if (OnChargeChanged != null)
            { //Only fire this event if anyone actually subscribes to it
                OnChargeChanged.Invoke();
            }
        }

        /// <summary>
        /// Checks if the storage can supply the amount of charge directly requested
        /// </summary>
        public bool CanDeductCharge(float toDeduct)
        {
            if (Charge > toDeduct)
                return true;
            return false;
        }

        /// <summary>
        /// Deducts the requested charge from the energy storage
        /// </summary>
        public virtual void DeductCharge(float toDeduct)
        {
            _charge = Math.Max(0, Charge - toDeduct);
            LastChargeState = ChargeState.Discharging;
            LastChargeStateChange = DateTime.Now;
            ChargeChanged();
        }

        public virtual void AddCharge(float charge)
        {
            _charge = Math.Min(Capacity, Charge + charge);
            LastChargeState = ChargeState.Charging;
            LastChargeStateChange = DateTime.Now;
            ChargeChanged();
        }

        /// <summary>
        ///     Returns the amount of energy that can be taken in by this storage in the specified amount of time.
        /// </summary>
        public float RequestCharge(float frameTime)
        {
            return Math.Min(ChargeRate * frameTime, Capacity - Charge);
        }

        /// <summary>
        ///     Returns the amount of energy available for discharge in the specified amount of time.
        /// </summary>
        public float AvailableCharge(float frameTime)
        {
            return Math.Min(DistributionRate * frameTime, Charge);
        }

        /// <summary>
        ///     Tries to deduct a wattage over a certain amount of time.
        /// </summary>
        /// <param name="wattage">The wattage of the power drain.</param>
        /// <param name="frameTime">The amount of time in this "frame".</param>
        /// <returns>True if the amount of energy was deducted, false.</returns>
        public bool TryDeductWattage(float wattage, float frameTime)
        {
            var avail = AvailableCharge(frameTime);
            if (avail < wattage * frameTime)
            {
                return false;
            }

            DeductCharge(wattage * frameTime);
            return true;
        }

        public ChargeState GetChargeState()
        {
            return GetChargeState(TimeSpan.FromSeconds(1));
        }

        public ChargeState GetChargeState(TimeSpan timeout)
        {
            if (LastChargeStateChange + timeout > DateTime.Now)
            {
                return LastChargeState;
            }
            return ChargeState.Still;
        }

        public void ChargePowerTick(float frameTime)
        {
            if (Full)
            {
                return;
            }
            AddCharge(RequestCharge(frameTime));
        }

        /// <inheritdoc />
        public void Examine(FormattedMessage message)
        {
            var loc = IoCManager.Resolve<ILocalizationManager>();

            var chargePercent = Math.Round(100*Charge/Capacity, 2);
            message.AddMarkup(loc.GetString(
                "[color=yellow]Charge: {0}J / {1}J ({2}%)\nRate: {3}W IN, {4}W OUT[/color]",
                Math.Round(Charge, 2), Capacity, chargePercent, ChargeRate, DistributionRate));
        }
    }
}
