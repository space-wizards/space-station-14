using Content.Shared.Clothing.Components;
using Content.Shared.Foldable;
using Content.Shared.Inventory;
using Content.Shared.Item;

namespace Content.Shared.Clothing.EntitySystems;

public sealed class FoldableClothingSystem : EntitySystem
{
    [Dependency] private readonly ClothingSystem _clothingSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedItemSystem _itemSystem = default!;

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
        if (!TryComp<ClothingComponent>(ent.Owner, out var clothingComp) ||
            !TryComp<ItemComponent>(ent.Owner, out var itemComp))
            return;

        if (args.IsFolded)
        {
            if (ent.Comp.FoldedSlots.HasValue)
                _clothingSystem.SetSlots(ent.Owner, ent.Comp.FoldedSlots.Value, clothingComp);

            if (ent.Comp.FoldedEquippedPrefix != null)
                _clothingSystem.SetEquippedPrefix(ent.Owner, ent.Comp.FoldedEquippedPrefix, clothingComp);

            if (ent.Comp.FoldedHeldPrefix != null)
                _itemSystem.SetHeldPrefix(ent.Owner, ent.Comp.FoldedHeldPrefix, false, itemComp);

            if (TryComp<HideLayerClothingComponent>(ent.Owner, out var hideLayerComp))
                hideLayerComp.Slots = ent.Comp.FoldedHideLayers;

        }
        else
        {
            if (ent.Comp.UnfoldedSlots.HasValue)
                _clothingSystem.SetSlots(ent.Owner, ent.Comp.UnfoldedSlots.Value, clothingComp);

            if (ent.Comp.FoldedEquippedPrefix != null)
                _clothingSystem.SetEquippedPrefix(ent.Owner, null, clothingComp);

            if (ent.Comp.FoldedHeldPrefix != null)
                _itemSystem.SetHeldPrefix(ent.Owner, null, false, itemComp);

            if (TryComp<HideLayerClothingComponent>(ent.Owner, out var hideLayerComp))
                hideLayerComp.Slots = ent.Comp.UnfoldedHideLayers;

        }
    }
}
