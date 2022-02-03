using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;


namespace Content.Server.Chemistry.Components
{
    [RegisterComponent]
    public class ReagentTankComponent : Component
    {
        [DataField("transferAmount")]
        [ViewVariables(VVAccess.ReadWrite)]
        public FixedPoint2 TransferAmount { get; set; } = FixedPoint2.New(10);

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
