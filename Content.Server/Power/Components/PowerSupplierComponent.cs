#nullable enable
using Content.Server.NodeContainer.NodeGroups;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Power.Components
{
    [RegisterComponent]
    public class PowerSupplierComponent : BasePowerNetComponent
    {
        public override string Name => "PowerSupplier";

        [ViewVariables(VVAccess.ReadWrite)]
        public int SupplyRate { get => _supplyRate; set => SetSupplyRate(value); }
        [DataField("supplyRate")]
        private int _supplyRate;

        protected override void AddSelfToNet(IPowerNet powerNet)
        {
            powerNet.AddSupplier(this);
        }

        protected override void RemoveSelfFromNet(IPowerNet powerNet)
        {
            powerNet.RemoveSupplier(this);
        }

        private void SetSupplyRate(int newSupplyRate)
        {
            Net.UpdateSupplierSupply(this, SupplyRate, newSupplyRate);
            _supplyRate = newSupplyRate;
        }
    }
}
