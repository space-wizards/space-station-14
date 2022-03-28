using System.Linq;
using Content.Client.Hands;
using Content.Client.Inventory;
using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface;

namespace Content.Client.UserInterface.Controllers;

public sealed partial class InventoryUIController : UIController
{
    [UISystemDependency] private readonly ClientInventorySystem _inventorySystem = default!;
    private readonly Dictionary<string, ItemSlotButtonContainer> _slotGroups = new();


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
        _inventorySystem.OnSlotAdded += AddSlot;
        _inventorySystem.OnSlotRemoved += RemoveSlot;
        _inventorySystem.OnLinkInventory += LoadSlots;
        _inventorySystem.OnUnlinkInventory += UnloadSlots;
    }
    private void OnInventorySystemDeactivate()
    {
    }


    private void AddSlot(ClientInventorySystem.SlotData data)
    {
        if(!_slotGroups.TryGetValue(data.SlotGroup, out var slotGroup)) return;
        var button = new ItemSlotButton(data);
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
        foreach ( var slotGroup in _slotGroups.Values)
        {
            slotGroup.ClearButtons();
        }
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
