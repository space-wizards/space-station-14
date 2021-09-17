using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Repairable
{
    [RegisterComponent]
    public class RepairableComponent : Component
    {
        public override string Name => "Repairable";

        [ViewVariables(VVAccess.ReadWrite)] [DataField("fuelCost")]
        public int FuelCost = 5;

        [ViewVariables(VVAccess.ReadWrite)] [DataField("doAfterDelay")]
        public int DoAfterDelay = 1;
    }
}
