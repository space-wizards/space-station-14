using Content.Client.UserInterface.Systems.Hands;
using Content.Client.UserInterface.Systems.Hands.Controls;
using Content.Client.UserInterface.Systems.Inventory;
using Content.Client.UserInterface.Systems.Inventory.Controls;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.UserInterface.Systems.Hotbar;

public sealed class HotbarUIController : UIController
{
    [Dependency] private InventoryUIController _inventory = default!;
    [Dependency] private HandsUIController _hands = default!;
    private ItemStatusPanel? _handStatus;
    private ItemSlotButtonContainer? _inventoryBar;


    public void Setup(HandsContainer handsContainer, ItemSlotButtonContainer inventoryBar, ItemStatusPanel handStatus)
    {
        _hands.RegisterHandContainer(handsContainer);
        _handStatus = handStatus;
        _inventoryBar = inventoryBar;
    }
}
