using Content.Client.UserInterface.Systems.Hands;
using Content.Client.UserInterface.Systems.Hands.Controls;
using Content.Client.UserInterface.Systems.Inventory;
using Content.Client.UserInterface.Systems.Inventory.Controls;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.UserInterface.Systems.Hotbar;

public sealed class HotbarUIController : UIController
{
    private InventoryUIController? _inventory;
    private HandsUIController? _hands;

    public void Setup(HandsContainer handsContainer, ItemSlotButtonContainer inventoryBar, ItemStatusPanel handStatus)
    {
        _inventory = UIManager.GetUIController<InventoryUIController>();
        _hands = UIManager.GetUIController<HandsUIController>();
        _hands.RegisterHandContainer(handsContainer);
        _inventory.RegisterInventoryBarContainer(inventoryBar);
    }
}
