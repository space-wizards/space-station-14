#nullable enable
using Content.Server.Battery.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Power.Components
{
    /// <summary>
    ///     Uses charge from a <see cref="BatteryComponent"/> to supply power via a <see cref="PowerSupplierComponent"/>.
    /// </summary>
    [RegisterComponent]
    public class BatteryDischargerComponent : Component
    {
        public override string Name => "BatteryDischarger";

        [ViewVariables]
        [ComponentDependency] private BatteryComponent? _battery = default!;

        [ViewVariables]
        [ComponentDependency] private PowerSupplierComponent? _supplier = default!;

        [ViewVariables(VVAccess.ReadWrite)]
        public int ActiveSupplyRate { get => _activeSupplyRate; set => SetActiveSupplyRate(value); }

        [DataField("activeSupplyRate")]
        private int _activeSupplyRate = 50;

        public override void Initialize()
        {
            base.Initialize();
            Owner.EnsureComponentWarn<PowerSupplierComponent>();
            UpdateSupplyRate();
        }

        public void Update(float frameTime)
        {
            if (_battery == null)
                return;

            //Simplified implementation - if the battery is empty, and charge is being added to the battery
            //at a lower rate that this is using it, the charge is used without creating power supply.
            _battery.CurrentCharge -= ActiveSupplyRate * frameTime;
            UpdateSupplyRate();
        }

        private void UpdateSupplyRate()
        {
            if (_battery == null)
                return;

            if (_battery.BatteryState == BatteryState.Empty)
            {
                SetSupplierSupplyRate(0);
            }
            else
            {
                SetSupplierSupplyRate(ActiveSupplyRate);
            }
        }

        private void SetSupplierSupplyRate(int newSupplierSupplyRate)
        {
            if (_supplier == null)
                return;

            if (_supplier.SupplyRate != newSupplierSupplyRate)
            {
                _supplier.SupplyRate = newSupplierSupplyRate;
            }
        }

        private void SetActiveSupplyRate(int newEnabledSupplyRate)
        {
            _activeSupplyRate = newEnabledSupplyRate;
            UpdateSupplyRate();
        }
    }
}
