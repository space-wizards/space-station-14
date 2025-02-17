// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Containers.ItemSlots;

namespace Content.Shared.Backmen.Economy.ATM;

public abstract class SharedATMSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AtmComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<AtmComponent, ComponentRemove>(OnComponentRemove);
    }
    private void OnComponentInit(EntityUid uid, AtmComponent component, ComponentInit args)
    {
        _itemSlotsSystem.AddItemSlot(uid, AtmComponent.IdCardSlotId, component.IdCardSlot);
    }

    private void OnComponentRemove(EntityUid uid, AtmComponent component, ComponentRemove args)
    {
        _itemSlotsSystem.RemoveItemSlot(uid, component.IdCardSlot);
    }
}
