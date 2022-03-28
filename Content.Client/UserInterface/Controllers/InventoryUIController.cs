using Content.Client.Hands;
using Content.Client.Inventory;
using Content.Client.UserInterface.Controls;
using Content.Shared.Input;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Input;

namespace Content.Client.UserInterface.Controllers;

public sealed partial class InventoryUIController : UIController
{
    [UISystemDependency] private readonly ClientInventorySystem _inventorySystem = default!;
    private readonly Dictionary<string, ItemSlotButtonContainer> _slotGroups = new();
    private Action<string>? _onInventoryActivate = null;
    private Action<string>? _onInventoryStorageActivate = null;

    //Neuron Activation
    public override void OnSystemLoaded(IEntitySystem system)
    {
        Logger.Debug("NEURON ACTIVATED");
        switch (system)
        {
            case HandsSystem:
                OnHandsSystemActivate();
                return;
            case ClientInventorySystem:
                OnInventorySystemActivate();
                return;
        }
    }
    //Neuron Deactivation
    public override void OnSystemUnloaded(IEntitySystem system)
    {
        switch (system)
        {
            case HandsSystem:
                OnHandsSystemDeactivate();
                return;
            case ClientInventorySystem:
                OnInventorySystemDeactivate();
                return;
        }
    }

    private void OnInventorySystemActivate()
    {
        _onInventoryActivate += _inventorySystem.UIInventoryActivate;
        _onInventoryStorageActivate += _inventorySystem.UIInventoryStorageActivate;
        _inventorySystem.OnSlotAdded += AddSlot;
        _inventorySystem.OnSlotRemoved += RemoveSlot;
        _inventorySystem.OnLinkInventory += LoadSlots;
        _inventorySystem.OnUnlinkInventory += UnloadSlots;
        _inventorySystem.OnSpriteUpdate += SpriteUpdated;
    }

    private void OnInventorySystemDeactivate()
    {
        _onInventoryActivate -= _inventorySystem.UIInventoryActivate;
        _onInventoryStorageActivate -= _inventorySystem.UIInventoryStorageActivate;
        _inventorySystem.OnSlotAdded -= AddSlot;
        _inventorySystem.OnSlotRemoved -= RemoveSlot;
        _inventorySystem.OnLinkInventory -= LoadSlots;
        _inventorySystem.OnUnlinkInventory -= UnloadSlots;
        _inventorySystem.OnSpriteUpdate -= SpriteUpdated;
    }

    private void OnItemPressed(GUIBoundKeyEventArgs args, ItemSlotControl control)
    {
        if (args.Function == EngineKeyFunctions.UIClick)
        {
            _onInventoryActivate?.Invoke(control.SlotName);
        }
        else if (args.Function == ContentKeyFunctions.ActivateItemInWorld)
        {
            _onInventoryStorageActivate?.Invoke(control.SlotName);
        }
    }

    private void AddSlot(ClientInventorySystem.SlotData data)
    {
        if(!_slotGroups.TryGetValue(data.SlotGroup, out var slotGroup)) return;
        var button = new ItemSlotButton(data);
        button.OnPressed += OnItemPressed;
        slotGroup.AddChild(button);
        button.SlotName = data.SlotName;
    }

    private void RemoveSlot(ClientInventorySystem.SlotData data)
    {
        if (!_slotGroups.TryGetValue(data.SlotGroup, out var slotGroup)) return;
        slotGroup.RemoveButton(data.SlotName);
    }

    private void LoadSlots(ClientInventoryComponent? clientInv)
    {
        if (clientInv == null) return;
        foreach (var slotData in clientInv.SlotData.Values)
        {
            AddSlot(slotData);
        }
    }

    private void UnloadSlots(ClientInventoryComponent? clientInv)
    {
        foreach (var slotGroup in _slotGroups.Values)
        {
            slotGroup.ClearButtons();
        }
    }

    private void SpriteUpdated(string slotGroup, string slotName, ISpriteComponent? sprite)
    {
        if (!_slotGroups.TryGetValue(slotGroup, out var group) ||
            !group.TryGetButton(slotName, out var button))
        {
            return;
        }

        button.SpriteView.Sprite = sprite;
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
