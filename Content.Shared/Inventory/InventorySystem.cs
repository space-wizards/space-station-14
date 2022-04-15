using Content.Shared.Inventory.Events;

namespace Content.Shared.Inventory;

public partial class InventorySystem
{

    public override void Initialize()
    {
        base.Initialize();
        InitializeEquip();
        InitializeRelay();
        InitializeInventorySlot();

        SubscribeLocalEvent<InventoryComponent, ComponentInit>(OnInvInit);
    }

    private void OnInvInit(EntityUid uid, InventoryComponent component, ComponentInit args)
    {
        if(!_prototypeManager.TryIndex<InventoryTemplatePrototype>(component.TemplateId, out var template)) return;

        for (int i = 0; i < template.Slots.Length; i++)
        {
            for (int j = i+1; j < template.Slots.Length; j++)
            {
                if (template.Slots[i].ConflictsWith(template.Slots[j]))
                {
                    Logger.Error($"Found slotdef conflict for slots {template.Slots[i].Name} and {template.Slots[j].Name}!");
                }
            }
        }
    }
}
