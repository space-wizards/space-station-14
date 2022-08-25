using Content.Client.Gameplay;
using Content.Client.Inventory;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Inventory.Controls;
using Content.Client.UserInterface.Systems.Inventory.Windows;
using Content.Shared.Input;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Utility;
using static Content.Client.Inventory.ClientInventorySystem;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.UserInterface.Systems.Inventory;

public sealed class InventoryUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>,IOnSystemChanged<ClientInventorySystem>
{
    [UISystemDependency] private readonly ClientInventorySystem _inventorySystem = default!;
    private ClientInventoryComponent? _playerInventory;
    private readonly Dictionary<string, ItemSlotButtonContainer> _slotGroups = new();
    private readonly Dictionary<(string group, string slot), (ISpriteComponent sprite, bool showStorage)> _sprites = new();

    private InventoryWindow? _inventoryWindow;
    private MenuButton? _inventoryButton;

    public void OnStateEntered(GameplayState state)
    {
        DebugTools.Assert(_inventoryWindow == null);
        _inventoryWindow = UIManager.CreateWindow<InventoryWindow>();
        _inventoryButton = UIManager.GetActiveUIWidget<MenuBar.Widgets.MenuBar>().InventoryButton;
        LayoutContainer.SetAnchorPreset(_inventoryWindow,LayoutContainer.LayoutPreset.Center);
        _inventoryWindow.OnClose += () => { _inventoryButton.Pressed = false; };
        _inventoryWindow.OnOpen += () => { _inventoryButton.Pressed = true; };

        //bind open inventory key to OpenInventoryMenu;
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenInventoryMenu, InputCmdHandler.FromDelegate(_ => ToggleInventoryMenu()))
            .Register<ClientInventorySystem>();
        _inventoryButton.OnPressed += InventoryButtonPressed;
    }

    public void OnStateExited(GameplayState state)
    {
        _inventoryWindow?.DisposeAllChildren();
        _inventoryWindow = null;
        CommandBinds.Unregister<ClientInventorySystem>();

        if (_inventoryButton == null)
            return;
        _inventoryButton.OnPressed -= InventoryButtonPressed;
        _inventoryButton.Pressed = false;
        _inventoryButton = null;
    }

    private void InventoryButtonPressed(ButtonEventArgs args)
    {
        ToggleInventoryMenu();
    }

    private void UpdateInventoryWindow(ClientInventoryComponent? clientInv)
    {
        if (clientInv == null)
        {
            _inventoryWindow!.InventoryButtons.ClearButtons();
            return;
        }
        foreach (var (_,data) in clientInv.SlotData)
        {
            if (!data.ShowInWindow)
                continue;

            var button = new SlotButton(data);
            button.Pressed += ItemPressed;
            button.StoragePressed += StoragePressed;

            _inventoryWindow!.InventoryButtons.AddButton(button, data.ButtonOffset);

            if (!_sprites.TryGetValue((data.SlotGroup, data.SlotName), out var tuple))
                continue;

            var update = new SlotSpriteUpdate(data.SlotGroup, data.SlotName, tuple.sprite, tuple.showStorage);
            SpriteUpdated(update);
        }
    }

    public void ToggleInventoryMenu()
    {
        UpdateInventoryWindow(_playerInventory);
        if (_inventoryWindow!.IsOpen)
        {
            _inventoryWindow!.Close();
            return;
        }
        _inventoryWindow.Open();
    }

    // Neuron Activation
    public void OnSystemLoaded(ClientInventorySystem system)
    {
        _inventorySystem.OnSlotAdded += AddSlot;
        _inventorySystem.OnSlotRemoved += RemoveSlot;
        _inventorySystem.OnLinkInventory += LoadSlots;
        _inventorySystem.OnUnlinkInventory += UnloadSlots;
        _inventorySystem.OnSpriteUpdate += SpriteUpdated;
    }

    // Neuron Deactivation
    public void OnSystemUnloaded(ClientInventorySystem system)
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
