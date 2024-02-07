using Content.Shared.Clothing.Components;
using Content.Shared.Foldable;
using Content.Shared.Inventory;

namespace Content.Shared.Clothing.EntitySystems;

public sealed class FoldableClothingSystem : EntitySystem
{
    [Dependency] private readonly ClothingSystem _clothingSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FoldableClothingComponent, FoldAttemptEvent>(OnFoldAttempt);
        SubscribeLocalEvent<FoldableClothingComponent, FoldedEvent>(OnFolded);
    }

    private void OnFoldAttempt(Entity<FoldableClothingComponent> ent, ref FoldAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        // allow folding while equipped if allowed slots are the same:
        // e.g. flip a hat backwards while on your head
        if (_inventorySystem.TryGetContainingSlot(ent.Owner, out var slot) &&
            !ent.Comp.FoldedSlots.Equals(ent.Comp.UnfoldedSlots))
            args.Cancelled = true;
    }

    private void OnFolded(Entity<FoldableClothingComponent> ent, ref FoldedEvent args)
    {
        if (TryComp<ClothingComponent>(ent.Owner, out var clothingComp))
        {
            if (args.IsFolded && ent.Comp.FoldedSlots.HasValue)
                _clothingSystem.SetSlots(ent.Owner, ent.Comp.FoldedSlots.Value, clothingComp);
            else if (!args.IsFolded && ent.Comp.UnfoldedSlots.HasValue)
                _clothingSystem.SetSlots(ent.Owner, ent.Comp.UnfoldedSlots.Value, clothingComp);
        }
    }
}
