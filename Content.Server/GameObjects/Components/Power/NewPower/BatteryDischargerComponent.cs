using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.NewPower
{
    /// <summary>
    ///     Pulls charge from a <see cref="BatteryComponent"/> to supply power with a <see cref="PowerSupplierComponent"/>.
    /// </summary>
    [RegisterComponent]
    public class BatteryDischargerComponent : Component
    {
        public override string Name => "BatteryDischarger";

        /// <summary>
        ///     The battery that this is trying to take power from.
        /// </summary>
        [ViewVariables]
        private BatteryComponent _battery;

        /// <summary>
        ///     The supplier power is released with.
        /// </summary>
        [ViewVariables]
        private PowerSupplierComponent _supplier;

        /// <summary>
        ///     The amount of power discharged by the supplier when the battery has power.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int EnabledSupplyRate { get => _enabledSupplyRate; set => SetEnabledSupplyRate(value); }
        private int _enabledSupplyRate;

        /// <summary>
        ///     Whether or not the battery has charge to allow the supplier to give
        ///     power to its powernet.
        /// </summary>
        [ViewVariables]
        private bool _hasCharge;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _enabledSupplyRate, "enabledDrawRate", 50);
        }

        public override void Initialize()
        {
            base.Initialize();
            _battery = Owner.GetComponent<BatteryComponent>();
            _supplier = Owner.GetComponent<PowerSupplierComponent>();
            _hasCharge = false; //starts out off, then updated to determine if it should be on
            UpdateSupplyRate();
        }

        public void Update(float frameTime)
        {
            //naive implementation - if the battery is empty, and charge is being added to the battery
            //at a lower rate that this is using it, the charge is used without creating supply.
            _battery.CurrentCharge -= EnabledSupplyRate * frameTime;
            UpdateSupplyRate();
        }

        private void UpdateSupplyRate()
        {
            var newHasCharge = _battery.BatteryState != BatteryState.Empty; //this supplies power if battery is not empty
            if (_hasCharge != newHasCharge) //if whether we have charge changed, update supply rate
            {
                _hasCharge = newHasCharge;
                if (_hasCharge)
                {
                    _supplier.SupplyRate += EnabledSupplyRate;
                }
                else
                {
                    _supplier.SupplyRate -= EnabledSupplyRate;
                }
            }
        }

        private void SetEnabledSupplyRate(int newEnabledSupplyRate)
        {
            _enabledSupplyRate = newEnabledSupplyRate;
            if (_hasCharge)
            {
                _supplier.SupplyRate += newEnabledSupplyRate - EnabledSupplyRate; //update supply if currently supplying
            }
        }
    }
}
