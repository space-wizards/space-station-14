using Content.Shared.Chemistry;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

#nullable enable

namespace Content.Server.GameObjects.Components.Chemistry
{
    [RegisterComponent]
    public class ReagentTankComponent : Component
    {
        public override string Name => "ReagentTank";

        [ViewVariables(VVAccess.ReadWrite)]
        public ReagentUnit TransferAmount { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public ReagentTankType TankType { get; set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, c => c.TransferAmount, "transferAmount", ReagentUnit.New(10));
            serializer.DataField(this, c => c.TankType, "tankType", ReagentTankType.Unspecified);
        }
    }

    public enum ReagentTankType : byte
    {
        Unspecified,
        Fuel
    }
}
