using Content.Shared.Access.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Containers;

namespace Content.Shared.LawChips.Judge;

public abstract class SharedJudgeInterfaceSystem : EntitySystem
{
    [Dependency] protected readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] protected readonly ItemSlotsSystem _items = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JudgeInterfaceComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<JudgeInterfaceComponent, ComponentRemove>(OnComponentRemove);
    }

    private void OnComponentInit(Entity<JudgeInterfaceComponent> ent, ref ComponentInit args)
    {
        _items.AddItemSlot(ent.Owner, JudgeInterfaceComponent.JudgeInterfaceLawChipSlotId, ent.Comp.ChipSlot);
    }

    private void OnComponentRemove(Entity<JudgeInterfaceComponent> ent, ref ComponentRemove args)
    {
        _items.RemoveItemSlot(ent.Owner, ent.Comp.ChipSlot);
    }
}
