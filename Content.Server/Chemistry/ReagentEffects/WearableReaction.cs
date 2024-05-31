using Content.Shared.Inventory;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects;

/// <summary>
/// A reaction effect that consumes the required amount of reagent and spawns PrototypeID in the
/// entity's Slot. Used to implement the water droplet effect for arachnids.
/// </summary>
public sealed partial class WearableReaction : ReagentEffect
{
    /// <summary>
    /// Minimum quantity of reagent required to trigger this effect.
    /// </summary>
    [DataField]
    public float AmountThreshold = 1f;

    /// <summary>
    /// Slot to spawn the item into.
    /// </summary>
    [DataField(required: true)]
    public string Slot;

    /// <summary>
    /// Prototype ID of item to spawn.
    /// </summary>
    [DataField(required: true)]
    public string PrototypeID;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => null;

    public override void Effect(ReagentEffectArgs args)
    {
        if (args.Reagent == null || args.Quantity < AmountThreshold)
            return;

        // SpawnItemInSlot returns false if slot is already occupied
        if (args.EntityManager.System<InventorySystem>().SpawnItemInSlot(args.SolutionEntity, Slot, PrototypeID))
            args.Source?.RemoveReagent(args.Reagent.ID, AmountThreshold);
    }
}
