using System.Linq;
using System.Numerics;
using Content.Client.Gameplay;
using Content.Client.Hands.Systems;
using Content.Client.Inventory;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Client.UserInterface.Systems.Inventory.Controls;
using Content.Client.UserInterface.Systems.Inventory.Widgets;
using Content.Client.UserInterface.Systems.Inventory.Windows;
using Content.Shared.Hands.Components;
using Content.Shared.Input;
using Content.Shared.Storage;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Utility;
using static Content.Client.Inventory.ClientInventorySystem;

namespace Content.Client.UserInterface.Systems.Inventory;

public sealed class InventoryUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>,
    IOnSystemChanged<ClientInventorySystem>, IOnSystemChanged<HandsSystem>
{
    [Dependency] private readonly IEntityManager _entities = default!;

    [UISystemDependency] private readonly ClientInventorySystem _inventorySystem = default!;
    [UISystemDependency] private readonly HandsSystem _handsSystem = default!;
    [UISystemDependency] private readonly ContainerSystem _container = default!;

    private EntityUid? _playerUid;
    private InventorySlotsComponent? _playerInventory;
    private readonly Dictionary<string, ItemSlotButtonContainer> _slotGroups = new();

    private StrippingWindow? _strippingWindow;
    private ItemSlotButtonContainer? _inventoryHotbar;
    private SlotButton? _inventoryButton;

    private SlotControl? _lastHovered;

    public override void Initialize()
    {
        base.Initialize();

        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += OnScreenLoad;
    }

    private void OnScreenLoad()
    {
        if (UIManager.ActiveScreen == null)
            return;

        var inventoryGui = UIManager.GetActiveUIWidget<InventoryGui>();
        RegisterInventoryButton(inventoryGui.InventoryButton);
    }

    public void OnStateEntered(GameplayState state)
    {
        DebugTools.Assert(_strippingWindow == null);
        _strippingWindow = UIManager.CreateWindow<StrippingWindow>();
        LayoutContainer.SetAnchorPreset(_strippingWindow, LayoutContainer.LayoutPreset.Center);

        //bind open inventory key to OpenInventoryMenu;
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenInventoryMenu, InputCmdHandler.FromDelegate(_ => ToggleInventoryBar()))
            .Register<ClientInventorySystem>();
    }

    public void OnStateExited(GameplayState state)
    {
        if (_strippingWindow != null)
        {
            _strippingWindow.Dispose();
            _strippingWindow = null;
        }

        if (_inventoryHotbar != null)
        {
            _inventoryHotbar.Visible = false;
        }

        CommandBinds.Unregister<ClientInventorySystem>();
    }

    private SlotButton CreateSlotButton(SlotData data)
    {
        var button = new SlotButton(data);
        button.Pressed += ItemPressed;
        button.StoragePressed += StoragePressed;
        button.Hover += SlotButtonHovered;

        return button;
    }

    public void RegisterInventoryBarContainer(ItemSlotButtonContainer inventoryHotbar)
    {
        _inventoryHotbar = inventoryHotbar;
    }

    public void RegisterInventoryButton(SlotButton? button)
    {
        if (_inventoryButton != null)
        {
            _inventoryButton.Pressed -= InventoryButtonPressed;
        }

        if (button != null)
        {
            _inventoryButton = button;
            _inventoryButton.Pressed += InventoryButtonPressed;
        }
    }

    private void InventoryButtonPressed(GUIBoundKeyEventArgs args, SlotControl control)
    {
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        ToggleInventoryBar();
    }

    private void UpdateInventoryHotbar(InventorySlotsComponent? clientInv)
    {
        if (clientInv == null)
        {
            _inventoryHotbar?.ClearButtons();
            return;
        }

        foreach (var (_, data) in clientInv.SlotData)
        {
            if (!data.ShowInWindow || !_slotGroups.TryGetValue(data.SlotGroup, out var container))
                continue;

            if (!container.TryGetButton(data.SlotName, out var button))
            {
                button = CreateSlotButton(data);
                container.AddButton(button);
            }

            var showStorage = _entities.HasComponent<StorageComponent>(data.HeldEntity);
            var update = new SlotSpriteUpdate(data.HeldEntity, data.SlotGroup, data.SlotName, showStorage);
            SpriteUpdated(update);
        }

        if (_inventoryHotbar == null)
            return;

        var clothing = clientInv.SlotData.Where(p => !p.Value.HasSlotGroup).ToList();

        if (_inventoryButton != null)
            _inventoryButton.Visible = clothing.Count != 0;
        if (clothing.Count == 0)
            return;

        foreach (var child in new List<Control>(_inventoryHotbar.Children))
        {
            if (child is not SlotControl)
                _inventoryHotbar.RemoveChild(child);
        }

        var maxWidth = clothing.Max(p => p.Value.ButtonOffset.X) + 1;
        var maxIndex = clothing.Select(p => GetIndex(p.Value.ButtonOffset)).Max();

        _inventoryHotbar.MaxColumns = maxWidth;
        _inventoryHotbar.Columns = maxWidth;

        for (var i = 0; i <= maxIndex; i++)
        {
            var index = i;
            if (clothing.FirstOrNull(p => GetIndex(p.Value.ButtonOffset) == index) is { } pair)
            {
                if (_inventoryHotbar.TryGetButton(pair.Key, out var slot))
                    slot.SetPositionLast();
            }
            else
            {
                _inventoryHotbar.AddChild(new Control
                {
                    MinSize = new Vector2(64, 64)
                });
            }
        }

        return;

        int GetIndex(Vector2i position)
        {
            return position.Y * maxWidth + position.X;
        }
    }

    private void UpdateStrippingWindow(InventorySlotsComponent? clientInv)
    {
        if (clientInv == null)
        {
            _strippingWindow!.InventoryButtons.ClearButtons();
            return;
        }

        foreach (var (_, data) in clientInv.SlotData)
        {
            if (!data.ShowInWindow)
                continue;

            if (!_strippingWindow!.InventoryButtons.TryGetButton(data.SlotName, out var button))
            {
                button = CreateSlotButton(data);
                _strippingWindow!.InventoryButtons.AddButton(button, data.ButtonOffset);
            }

            var showStorage = _entities.HasComponent<StorageComponent>(data.HeldEntity);
            var update = new SlotSpriteUpdate(data.HeldEntity, data.SlotGroup, data.SlotName, showStorage);
            SpriteUpdated(update);
        }
    }

    public void ToggleStrippingMenu()
    {
        UpdateStrippingWindow(_playerInventory);
        if (_strippingWindow!.IsOpen)
        {
            _strippingWindow!.Close();
            return;
        }

        _strippingWindow.Open();
    }

    public void ToggleInventoryBar()
    {
        if (_inventoryHotbar == null)
        {
            Logger.Warning("Tried to toggle inventory bar when none are assigned");
            return;
        }

        UpdateInventoryHotbar(_playerInventory);
        _inventoryHotbar.Visible = !_inventoryHotbar.Visible;
    }

    // Neuron Activation
    public void OnSystemLoaded(ClientInventorySystem system)
    {
        _inventorySystem.OnSlotAdded += AddSlot;
        _inventorySystem.OnSlotRemoved += RemoveSlot;
        _inventorySystem.OnLinkInventorySlots += LoadSlots;
        _inventorySystem.OnUnlinkInventory += UnloadSlots;
        _inventorySystem.OnSpriteUpdate += SpriteUpdated;
    }

    // Neuron Deactivation
    public void OnSystemUnloaded(ClientInventorySystem system)
    {
        _inventorySystem.OnSlotAdded -= AddSlot;
        _inventorySystem.OnSlotRemoved -= RemoveSlot;
        _inventorySystem.OnLinkInventorySlots -= LoadSlots;
        _inventorySystem.OnUnlinkInventory -= UnloadSlots;
        _inventorySystem.OnSpriteUpdate -= SpriteUpdated;
    }

    private void ItemPressed(GUIBoundKeyEventArgs args, SlotControl control)
    {
        var slot = control.SlotName;

        if (args.Function == EngineKeyFunctions.UIClick)
        {
            _inventorySystem.UIInventoryActivate(control.SlotName);
            args.Handle();
            return;
        }

        if (_playerInventory == null || _playerUid == null)
        {
            return;
        }

        if (args.Function == ContentKeyFunctions.ExamineEntity)
        {
            _inventorySystem.UIInventoryExamine(slot, _playerUid.Value);
        }
        else if (args.Function == EngineKeyFunctions.UseSecondary)
        {
            _inventorySystem.UIInventoryOpenContextMenu(slot, _playerUid.Value);
        }
        else if (args.Function == ContentKeyFunctions.ActivateItemInWorld)
        {
            _inventorySystem.UIInventoryActivateItem(slot, _playerUid.Value);
        }
        else if (args.Function == ContentKeyFunctions.AltActivateItemInWorld)
        {
            _inventorySystem.UIInventoryAltActivateItem(slot, _playerUid.Value);
        }
        else
        {
            return;
        }

        args.Handle();
    }

    private void StoragePressed(GUIBoundKeyEventArgs args, SlotControl control)
    {
        _inventorySystem.UIInventoryStorageActivate(control.SlotName);
    }

    private void SlotButtonHovered(GUIMouseHoverEventArgs args, SlotControl control)
    {
        UpdateHover(control);
        _lastHovered = control;
    }

    public void UpdateHover(SlotControl control)
    {
        var player = _playerUid;

        if (!control.MouseIsHovering ||
            _playerInventory == null ||
            !_entities.TryGetComponent<HandsComponent>(player, out var hands) ||
            hands.ActiveHandEntity is not { } held ||
            !_entities.TryGetComponent(held, out SpriteComponent? sprite) ||
            !_inventorySystem.TryGetSlotContainer(player.Value, control.SlotName, out var container, out var slotDef))
        {
            control.ClearHover();
            return;
        }

        // Set green / red overlay at 50% transparency
        var hoverEntity = _entities.SpawnEntity("hoverentity", MapCoordinates.Nullspace);
        var hoverSprite = _entities.GetComponent<SpriteComponent>(hoverEntity);
        var fits = _inventorySystem.CanEquip(player.Value, held, control.SlotName, out _, slotDef) &&
                   _container.CanInsert(held, container);

        hoverSprite.CopyFrom(sprite);
        hoverSprite.Color = fits ? new Color(0, 255, 0, 127) : new Color(255, 0, 0, 127);

        control.HoverSpriteView.SetEntity(hoverEntity);
    }

    private void AddSlot(SlotData data)
    {
        if (!_slotGroups.TryGetValue(data.SlotGroup, out var slotGroup))
            return;

        var button = CreateSlotButton(data);
        slotGroup.AddButton(button);
    }

    private void RemoveSlot(SlotData data)
    {
        if (!_slotGroups.TryGetValue(data.SlotGroup, out var slotGroup))
            return;

        slotGroup.RemoveButton(data.SlotName);
    }

    public void ReloadSlots()
    {
        _inventorySystem.ReloadInventory();
    }

    private void LoadSlots(EntityUid clientUid, InventorySlotsComponent clientInv)
    {
        UnloadSlots();
        _playerUid = clientUid;
        _playerInventory = clientInv;
        foreach (var slotData in clientInv.SlotData.Values)
        {
            AddSlot(slotData);
        }

        UpdateInventoryHotbar(_playerInventory);
    }

    private void UnloadSlots()
    {
        _playerUid = null;
        _playerInventory = null;
        foreach (var slotGroup in _slotGroups.Values)
        {
            slotGroup.ClearButtons();
        }
    }

    private void SpriteUpdated(SlotSpriteUpdate update)
    {
        var (entity, group, name, showStorage) = update;

        if (_strippingWindow?.InventoryButtons.GetButton(update.Name) is { } inventoryButton)
        {
            inventoryButton.SpriteView.SetEntity(entity);
            inventoryButton.StorageButton.Visible = showStorage;
        }

        if (_slotGroups.GetValueOrDefault(group)?.GetButton(name) is not { } button)
            return;

        button.SpriteView.SetEntity(entity);
        button.StorageButton.Visible = showStorage;
    }

    public bool RegisterSlotGroupContainer(ItemSlotButtonContainer slotContainer)
    {
        if (_slotGroups.TryAdd(slotContainer.SlotGroup, slotContainer))
            return true;

        return false;
    }

    public void RemoveSlotGroup(string slotGroupName)
    {
        _slotGroups.Remove(slotGroupName);
    }

    // Monkey Sees Action
    // Neuron Activation
    // Monkey copies code
    public void OnSystemLoaded(HandsSystem system)
    {
        _handsSystem.OnPlayerItemAdded += OnItemAdded;
        _handsSystem.OnPlayerItemRemoved += OnItemRemoved;
        _handsSystem.OnPlayerSetActiveHand += SetActiveHand;
    }

    public void OnSystemUnloaded(HandsSystem system)
    {
        _handsSystem.OnPlayerItemAdded -= OnItemAdded;
        _handsSystem.OnPlayerItemRemoved -= OnItemRemoved;
        _handsSystem.OnPlayerSetActiveHand -= SetActiveHand;
    }


    private void OnItemAdded(string name, EntityUid entity)
    {
        if (_lastHovered != null)
            UpdateHover(_lastHovered);
    }

    private void OnItemRemoved(string name, EntityUid entity)
    {
        if (_lastHovered != null)
            UpdateHover(_lastHovered);
    }

    private void SetActiveHand(string? handName)
    {
        if (_lastHovered != null)
            UpdateHover(_lastHovered);
    }
}
