using Content.Server.GameObjects.Components.Chemistry;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Cargo.RequestSpecifiers
{
    public class ReagentRequestSpecifier : RequestSpecifier
    {
        public string ReagentID { get; private set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(this, x => x.ReagentID, "reagent", "");
            if (string.IsNullOrEmpty(ReagentID))
            {
                Logger.Error($"Failed to serialize reagentID for {nameof(ReagentRequestSpecifier)}");
            }
        }

        public override int EntityToUnits(IEntity entity)
        {
            if (!entity.TryGetComponent(out SolutionContainerComponent solutionContainerComponent)) return 0;
            var quantity = solutionContainerComponent.Solution.GetReagentQuantity(ReagentID);
            if (quantity == 0) return 0;
            solutionContainerComponent.Solution.RemoveReagent(ReagentID, quantity);
            return quantity.Int();
        }
    }
}
