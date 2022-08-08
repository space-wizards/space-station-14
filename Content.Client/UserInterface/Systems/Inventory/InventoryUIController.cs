using Content.Client.Gameplay;
using Content.Client.Inventory;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Inventory.Controls;
using Content.Client.UserInterface.Systems.Inventory.Windows;
using Content.Shared.Input;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using static Content.Client.Inventory.ClientInventorySystem;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.UserInterface.Systems.Inventory;

public sealed partial class InventoryUIController : UIController, IOnStateEntered<GameplayState>
{
    [UISystemDependency] private readonly ClientInventorySystem _inventorySystem = default!;
    private ClientInventoryComponent? _playerInventory;
    private readonly Dictionary<string, ItemSlotButtonContainer> _slotGroups = new();
    private InventoryWindow? _inventoryWindow;
    private readonly Dictionary<(string group, string slot), (ISpriteComponent sprite, bool showStorage)> _sprites = new();
    private MenuButton InventoryButton => UIManager.GetActiveUIWidget<MenuBar.Widgets.MenuBar>().InventoryButton;

    public void OnStateEntered(GameplayState state)
    {
        //bind open inventory key to OpenInventoryMenu;
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenInventoryMenu, InputCmdHandler.FromDelegate(_ => ToggleInventoryMenu()))
            .Register<ClientInventorySystem>();

        UIManager.GetActiveUIWidget<MenuBar.Widgets.MenuBar>().InventoryButton.OnPressed += InventoryButtonPressed;
    }

    private void InventoryButtonPressed(ButtonEventArgs args)
    {
        ToggleInventoryMenu();
    }

    private void CreateInventoryWindow(ClientInventoryComponent? clientInv)
    {
        if (clientInv == null) return;

        _inventoryWindow = UIManager.CreateWindow<InventoryWindow>();
        if (_inventoryWindow == null)
            return;

        foreach (var (_,data) in clientInv.SlotData)
        {
            if (!data.ShowInWindow)
                continue;

            var button = new SlotButton(data);
            button.Pressed += ItemPressed;
            button.StoragePressed += StoragePressed;

            _inventoryWindow.InventoryButtons.AddButton(button, data.ButtonOffset);

            if (!_sprites.TryGetValue((data.SlotGroup, data.SlotName), out var tuple))
                continue;

            var update = new SlotSpriteUpdate(data.SlotGroup, data.SlotName, tuple.sprite, tuple.showStorage);
            SpriteUpdated(update);
        }

        _inventoryWindow.OnClose += OnInventoryClosed;
        InventoryButton.Pressed = true;
    }

    public void ToggleInventoryMenu()
    {
        if (_inventoryWindow != null)
        {
            _inventoryWindow.Close();
            return;
        }

        CreateInventoryWindow(_playerInventory);
    }

    private void OnInventoryClosed()
    {
        _inventoryWindow = null;
        InventoryButton.Pressed = false;
    }

    //Neuron Activation
    public override void OnSystemLoaded(IEntitySystem system)
    {
        switch (system)
        {
            case ClientInventorySystem:
                OnInventorySystemActivate();
                break;
        }
    }
    //Neuron Deactivation
    public override void OnSystemUnloaded(IEntitySystem system)
    {
        switch (system)
        {
            case ClientInventorySystem:
                OnInventorySystemDeactivate();
                break;
        }
    }

    private void OnInventorySystemActivate()
    {
        _inventorySystem.OnSlotAdded += AddSlot;
        _inventorySystem.OnSlotRemoved += RemoveSlot;
        _inventorySystem.OnLinkInventory += LoadSlots;
        _inventorySystem.OnUnlinkInventory += UnloadSlots;
        _inventorySystem.OnSpriteUpdate += SpriteUpdated;
    }

    private void OnInventorySystemDeactivate()
    {
        _inventorySystem.OnSlotAdded -= AddSlot;
        _inventorySystem.OnSlotRemoved -= RemoveSlot;
        _inventorySystem.OnLinkInventory -= LoadSlots;
        _inventorySystem.OnUnlinkInventory -= UnloadSlots;
        _inventorySystem.OnSpriteUpdate -= SpriteUpdated;
    }

    private void ItemPressed(GUIBoundKeyEventArgs args, SlotControl control)
    {
        var slot = control.SlotName;

        if (args.Function == EngineKeyFunctions.UIClick)
        {
            _inventorySystem.UIInventoryActivate(control.SlotName);
            return;
        }

        if (args.Function == ContentKeyFunctions.ActivateItemInWorld)
        {
            _inventorySystem.UIInventoryStorageActivate(control.SlotName);
            return;
        }

        if (_playerInventory == null)
        {
            return;
        }

        if (args.Function == ContentKeyFunctions.ExamineEntity)
        {
            _inventorySystem.UIInventoryExamine(slot, _playerInventory.Owner);
        }
        else if (args.Function == ContentKeyFunctions.OpenContextMenu)
        {
            _inventorySystem.UIInventoryOpenContextMenu(slot, _playerInventory.Owner);
        }
        else if (args.Function == ContentKeyFunctions.ActivateItemInWorld)
        {
            _inventorySystem.UIInventoryActivateItem(slot, _playerInventory.Owner);
        }
        else if (args.Function == ContentKeyFunctions.AltActivateItemInWorld)
        {
            _inventorySystem.UIInventoryAltActivateItem(slot, _playerInventory.Owner);
        }
    }

    private void StoragePressed(GUIBoundKeyEventArgs args, SlotControl control)
    {
        _inventorySystem.UIInventoryStorageActivate(control.SlotName);
    }

    private void AddSlot(SlotData data)
    {
        if(!_slotGroups.TryGetValue(data.SlotGroup, out var slotGroup)) return;
        var button = new SlotButton(data);
        button.Pressed += ItemPressed;
        button.StoragePressed += StoragePressed;
        slotGroup.AddButton(button);
        button.SlotName = data.SlotName;
    }

    private void RemoveSlot(SlotData data)
    {
        if (!_slotGroups.TryGetValue(data.SlotGroup, out var slotGroup)) return;
        slotGroup.RemoveButton(data.SlotName);
    }

    private void LoadSlots(ClientInventoryComponent clientInv)
    {
        _playerInventory = clientInv;
        foreach (var slotData in clientInv.SlotData.Values)
        {
            AddSlot(slotData);
        }
    }

    private void UnloadSlots()
    {
        _playerInventory = null;
        foreach (var slotGroup in _slotGroups.Values)
        {
            slotGroup.ClearButtons();
        }
    }

    private void SpriteUpdated(SlotSpriteUpdate update)
    {
        var (group, name, sprite, showStorage) = update;

        if (sprite == null)
        {
            _sprites.Remove((group, name));
        }
        else
        {
            _sprites[(group, name)] = (sprite, showStorage);
        }

        if (_inventoryWindow?.InventoryButtons.GetButton(update.Name) is { } inventoryButton)
        {
            inventoryButton.SpriteView.Sprite = sprite;
            inventoryButton.StorageButton.Visible = showStorage;
        }

        if (_slotGroups.GetValueOrDefault(group)?.GetButton(name) is not { } button)
            return;

        button.SpriteView.Sprite = sprite;
        button.StorageButton.Visible = showStorage;
    }

    public void BlockSlot(string slotName, bool blocked)
    {
    }
    public void HighlightSlot(string slotName, bool highlight)
    {
    }
    public bool RegisterSlotGroupContainer(ItemSlotButtonContainer slotContainer)
    {
        return _slotGroups.TryAdd(slotContainer.SlotGroup, slotContainer);
    }

    public void RemoveSlotGroup(string slotGroupName)
    {
        _slotGroups.Remove(slotGroupName);
    }
}
