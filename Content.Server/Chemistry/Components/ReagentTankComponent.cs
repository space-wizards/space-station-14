using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;


namespace Content.Server.Chemistry.Components
{
    [RegisterComponent]
    public class ReagentTankComponent : Component
    {
        public override string Name => "ReagentTank";

        [DataField("transferAmount")]
        [ViewVariables(VVAccess.ReadWrite)]
        public ReagentUnit TransferAmount { get; set; } = ReagentUnit.New(10);

        [DataField("tankType")]
        [ViewVariables(VVAccess.ReadWrite)]
        public ReagentTankType TankType { get; set; } = ReagentTankType.Unspecified;
    }

    public enum ReagentTankType : byte
    {
        Unspecified,
        Fuel
    }
}
