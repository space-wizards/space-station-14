using Content.Client.UserInterface.Controllers;
using Robust.Client.UserInterface;

namespace Content.Client.UserInterface.Controls;

public sealed class ItemSlotButtonContainer : ItemSlotUIContainer<ItemSlotControl>
{
    private readonly InventoryUIController _inventoryController;
    private string _slotGroup = "";
    public string SlotGroup { get => _slotGroup;
        set
        {
            _inventoryController.RemoveSlotGroup(SlotGroup);
            _slotGroup = value;
            _inventoryController.RegisterSlotGroupContainer(this);
        }}
    public ItemSlotButtonContainer()
    {
        _inventoryController = IoCManager.Resolve<IUIControllerManager>().GetController<InventoryUIController>();
    }

    ~ItemSlotButtonContainer()
    {
        _inventoryController.RemoveSlotGroup(SlotGroup);
    }

}
