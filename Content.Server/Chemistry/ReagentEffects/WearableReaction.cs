using Content.Shared.Inventory;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects
{
    public sealed partial class WearableReaction : ReagentEffect
    {

        [DataField]
        public float AmountThreshold = 1f;

        [DataField(required: true)]
        public string Slot;

        [DataField(required: true)]
        public string PrototypeID;

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;

        public override void Effect(ReagentEffectArgs args)
        {
            if (args.Reagent == null || args.Quantity < AmountThreshold)
                return;

            if (args.EntityManager.System<InventorySystem>().SpawnItemInSlot(args.SolutionEntity, Slot, PrototypeID))
                args.Source?.RemoveReagent(args.Reagent.ID, AmountThreshold);
        }
    }
}
