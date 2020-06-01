using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;

namespace Content.Server.GameObjects.Components.NewPower
{
    [RegisterComponent]
    public class PowerSupplierComponent : BasePowerComponent
    {
        public override string Name => "PowerSupplier";

        [ViewVariables(VVAccess.ReadWrite)]
        public int SupplyRate { get => _supplyRate; set => SetSupplyRate(value); }
        private int _supplyRate;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _supplyRate, "supplyRate", 100);
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
            PowerNet.UpdateSupplierSupply(this, SupplyRate, newSupplyRate);
            _supplyRate = newSupplyRate;
        }
    }
}
