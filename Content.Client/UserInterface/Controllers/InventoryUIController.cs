using Content.Client.Hands;
using Content.Client.Inventory;
using Content.Client.UserInterface.Controls;
using Robust.Client.State;
using Robust.Client.UserInterface;

namespace Content.Client.UserInterface.Controllers;

public sealed partial class InventoryUIController : UIController
{
    [Dependency] private IEntityManager _entityManager = default!;
    [UISystemDependency] private ClientInventorySystem? _inventorySystem = default!;
    private readonly Dictionary<string, ItemSlotUIContainer> _slotGroups = new();
    private ClientInventoryComponent? _playerInventoryComponent;
    public ClientInventoryComponent? PlayerInventory => _playerInventoryComponent;

    public void SetPlayerInvComponent(ClientInventoryComponent? clientInv)
    {
        _playerInventoryComponent = clientInv;
    }


    public void BlockSlot(string slotName, bool blocked)
    {
        if (_inventorySystem == null || _playerInventoryComponent == null) return;

    }

    public void HighlightSlot(string slotName, bool highlight)
    {
        if (_inventorySystem == null || _playerInventoryComponent == null) return;
        _inventorySystem.SetSlotHighlight(_playerInventoryComponent, slotName, highlight);
    }
    public bool RegisterSlotGroupContainer(ItemSlotUIContainer slotContainer)
    {
        return slotContainer.Name != null && _slotGroups.TryAdd(slotContainer.Name!, slotContainer);
    }

    public void RemoveSlotGroup(string slotGroupName)
    {
        _slotGroups.Remove(slotGroupName);
    }


    private void DebugTest()
    {
        if (_inventorySystem == null)
        {
            Logger.Debug("Null RIP");
            return;
        }
        Logger.Debug("Found");
    }
}
