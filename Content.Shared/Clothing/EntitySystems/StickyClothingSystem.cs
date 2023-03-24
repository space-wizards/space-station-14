using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Interaction.Components;
using Content.Shared.Item;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Shared.Clothing.EntitySystems;

public sealed class StickyClothingSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StickyClothingComponent, GotEquippedEvent>(OnGotEquipped);
    }

    private void OnGotEquipped(EntityUid uid, StickyClothingComponent component, GotEquippedEvent args)
    {
        if (!TryComp(uid, out ClothingComponent? clothing))
            return;

        // check if entity was actually used as clothing
        // not just taken in pockets or something
        var isCorrectSlot = clothing.Slots.HasFlag(args.SlotFlags);
        if (!isCorrectSlot) return;

        EnsureComp<UnremoveableComponent>(uid);
    }
}
