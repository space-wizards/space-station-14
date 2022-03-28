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
    private ClientInventoryComponent? _playerInventoryComponent;
    public ClientInventoryComponent? PlayerInventory => _playerInventoryComponent;


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
        _inventorySystem.OnSlotAdded += OnSlotAdded;
        _inventorySystem.OnSlotRemoved += OnSlotRemoved;
    }
    private void OnInventorySystemDeactivate()
    {
    }


    private void OnSlotAdded(ClientInventorySystem.SlotData data)
    {
        if(!_slotGroups.TryGetValue(data.SlotGroup, out var slotGroup)) return;
        var button = new ItemSlotButton(data);
        slotGroup.AddChild(button);
        button.SlotName = data.SlotName;
    }
    private void OnSlotRemoved(ClientInventorySystem.SlotData data)
    {
        if (!_slotGroups.TryGetValue(data.SlotGroup, out var slotGroup)) return;
        slotGroup.RemoveButton(data.SlotName);
    }


    public void SetPlayerInvComponent(ClientInventoryComponent? clientInv)
    {
        _playerInventoryComponent = clientInv;
    }
    public void BlockSlot(string slotName, bool blocked)
    {
        if (_playerInventoryComponent == null) return;

    }
    public void HighlightSlot(string slotName, bool highlight)
    {
        if (_playerInventoryComponent == null) return;
        _inventorySystem.SetSlotHighlight(_playerInventoryComponent, slotName, highlight);
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
