using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.NewPower
{
    /// <summary>
    ///     Adds electrical power to the <see cref="PowerNet"/> it is part of.
    /// </summary>
    [RegisterComponent]
    public class PowerSupplierComponent : BasePowerNetConnector
    {
        public override string Name => "PowerSupplier";

        /// <summary>
        ///     The amount of electrical power (Watts) being provided by this power supplier.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int SupplyRate { get => _supplyRate; set => SetSupplyRate(value); }
        private int _supplyRate;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _supplyRate, "supplyRate", 100);
        }

        /// <inheritdoc />
        protected override bool TryJoinPowerNet(PowerNet powerNet)
        {
            return powerNet.TryAddSupplier(this);
        }

        /// <inheritdoc />
        protected override void NotifyPowerNetOfLeaving()
        {
            PowerNet?.RemoveSupplier(this);
        }

        private void SetSupplyRate(int newSupply)
        {
            var oldSupply = _supplyRate;
            _supplyRate = newSupply;
            PowerNet?.UpdateSupplierSupply(this, oldSupply, newSupply);
        }
    }
}
