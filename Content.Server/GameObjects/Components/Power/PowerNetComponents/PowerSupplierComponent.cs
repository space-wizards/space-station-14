#nullable enable
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Power.PowerNetComponents
{
    [RegisterComponent]
    public class PowerSupplierComponent : BasePowerNetComponent
    {
        public override string Name => "PowerSupplier";

        [ViewVariables(VVAccess.ReadWrite)]
        public int SupplyRate { get => _supplyRate; set => SetSupplyRate(value); }
        private int _supplyRate;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _supplyRate, "supplyRate", 0);
        }

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
