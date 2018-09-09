using Content.Shared.GameObjects.Components.Power;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Serialization;
using SS14.Shared.Utility;
using System;
using SS14.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Content.Server.GameObjects.Components.Power
{
    /// <summary>
    /// Feeds energy from the powernet and may have the ability to supply back into it
    /// </summary>
    public class PowerStorageComponent : Component
    {
        public override string Name => "PowerStorage";

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
        /// </summary>
        [ViewVariables]
        public float Charge => _charge;
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

        private bool _chargepowernet = false;

        /// <summary>
        /// Do we distribute power into the powernet from our stores if the powernet requires it?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool ChargePowernet
        {
            get => _chargepowernet;
            set
            {
                _chargepowernet = value;
                if (Owner.TryGetComponent(out PowerNodeComponent node))
                {
                    if (node.Parent != null)
                        node.Parent.UpdateStorageType(this);
                }
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _capacity, "capacity", 10000);
            serializer.DataField(ref _charge, "charge", 0);
            serializer.DataField(ref _chargeRate, "chargerate", 1000);
            serializer.DataField(ref _distributionRate, "distributionrate", 1000);
            serializer.DataField(ref _chargepowernet, "chargepowernet", false);
        }

        public override void OnAdd()
        {
            base.OnAdd();

            if (!Owner.TryGetComponent(out PowerNodeComponent node))
            {
                Owner.AddComponent<PowerNodeComponent>();
                node = Owner.GetComponent<PowerNodeComponent>();
            }
            node.OnPowernetConnect += PowernetConnect;
            node.OnPowernetDisconnect += PowernetDisconnect;
            node.OnPowernetRegenerate += PowernetRegenerate;
        }

        public override void OnRemove()
        {
            if (Owner.TryGetComponent(out PowerNodeComponent node))
            {
                if (node.Parent != null)
                {
                    node.Parent.RemovePowerStorage(this);
                }

                node.OnPowernetConnect -= PowernetConnect;
                node.OnPowernetDisconnect -= PowernetDisconnect;
                node.OnPowernetRegenerate -= PowernetRegenerate;
            }

            base.OnRemove();
        }

        /// <summary>
        /// Checks if the storage can supply the amount of charge directly requested
        /// </summary>
        public bool CanDeductCharge(float todeduct)
        {
            if (Charge > todeduct)
                return true;
            return false;
        }

        /// <summary>
        /// Deducts the requested charge from the energy storage
        /// </summary>
        public void DeductCharge(float todeduct)
        {
            _charge = Math.Max(0, Charge - todeduct);
            LastChargeState = ChargeState.Discharging;
            LastChargeStateChange = DateTime.Now;
        }

        public void AddCharge(float charge)
        {
            _charge = Math.Min(Capacity, Charge + charge);
            LastChargeState = ChargeState.Charging;
            LastChargeStateChange = DateTime.Now;
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

        /// <summary>
        /// Node has become anchored to a powernet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventarg"></param>
        private void PowernetConnect(object sender, PowernetEventArgs eventarg)
        {
            eventarg.Powernet.AddPowerStorage(this);
        }

        /// <summary>
        /// Node has had its powernet regenerated
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventarg"></param>
        private void PowernetRegenerate(object sender, PowernetEventArgs eventarg)
        {
            eventarg.Powernet.AddPowerStorage(this);
        }

        /// <summary>
        /// Node has become unanchored from a powernet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventarg"></param>
        private void PowernetDisconnect(object sender, PowernetEventArgs eventarg)
        {
            eventarg.Powernet.RemovePowerStorage(this);
        }
    }
}
