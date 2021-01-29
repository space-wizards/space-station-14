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

            serializer.DataField(this, c => c.TransferAmount, "TransferAmount", ReagentUnit.New(10));
            serializer.DataField(this, c => c.TankType, "TankType", ReagentTankType.Unspecified);
        }
    }

    public enum ReagentTankType
    {
        Unspecified,
        Fuel
    }

    public struct ExplosionStrength
    {
        public int Dev;
        public int Heavy;
        public int Light;
        public int Flash;
    }

    /*
    - type: WeldingTankComponent
      explosionStrength:
        dev: 1
        heavy: 3
        light: 5
        flash: 7
     */
}
