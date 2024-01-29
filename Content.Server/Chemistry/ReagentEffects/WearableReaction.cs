using Content.Shared.Inventory;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects;

public sealed partial class WearableReaction : ReagentEffect
{
    [DataField]
    public float AmountThreshold = 1f;

    [DataField(required: true)]
    public string Slot;

    /// <summary>
    /// ID of item to spawn.
    /// </summary>
    [DataField(required: true)]
    public string PrototypeID;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    => null;

    /// <summary>
    /// Attempts to spawn an item in the indicated slot if reagent amount is over threshold.Then substracts threshold amount of reagent.
    /// Useful for situations where the reagent "becomes" an item entity on a player when reacting. 
    /// </summary>
    public override void Effect(ReagentEffectArgs args)
    {
        if (args.Reagent == null || args.Quantity < AmountThreshold)
            return;

        if (args.EntityManager.System<InventorySystem>().SpawnItemInSlot(args.SolutionEntity, Slot, PrototypeID))
            args.Source?.RemoveReagent(args.Reagent.ID, AmountThreshold);
    }
}
