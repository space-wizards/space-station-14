using Content.Client.UserInterface.Systems.Gameplay;
using Content.Client.UserInterface.Systems.Hands;
using Content.Client.UserInterface.Systems.Hands.Controls;
using Content.Client.UserInterface.Systems.Hotbar.Widgets;
using Content.Client.UserInterface.Systems.Inventory;
using Content.Client.UserInterface.Systems.Inventory.Controls;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.UserInterface.Systems.Hotbar;

public sealed class HotbarUIController : UIController
{
    private InventoryUIController? _inventory;
    private HandsUIController? _hands;

    public override void Initialize()
    {
        base.Initialize();

        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += OnScreenLoad;
    }

    private void OnScreenLoad()
    {
        ReloadHotbar();
    }

    public void Setup(HandsContainer handsContainer, ItemSlotButtonContainer inventoryBar, ItemStatusPanel handStatus)
    {
        _inventory = UIManager.GetUIController<InventoryUIController>();
        _hands = UIManager.GetUIController<HandsUIController>();
        _hands.RegisterHandContainer(handsContainer);
        _inventory.RegisterInventoryBarContainer(inventoryBar);
    }

    public void ReloadHotbar()
    {
        if (UIManager.ActiveScreen == null)
        {
            return;
        }

        var hotbar = UIManager.ActiveScreen.GetWidget<HotbarGui>();

        if (hotbar == null)
        {
            return;
        }

        foreach (var container in GetAllItemSlotContainers(hotbar))
        {
            // Yes, this is dirty.
            container.SlotGroup = container.SlotGroup;
        }

        _hands?.ReloadHands();
        _inventory?.ReloadSlots();
        _inventory?.RegisterInventoryBarContainer(hotbar.InventoryHotbar);
    }

    private IEnumerable<ItemSlotButtonContainer> GetAllItemSlotContainers(Control gui)
    {
        var result = new List<ItemSlotButtonContainer>();

        foreach (var child in gui.Children)
        {
            if (child is ItemSlotButtonContainer container)
            {
                result.Add(container);
            }

            result.AddRange(GetAllItemSlotContainers(child));
        }

        return result;
    }
}
