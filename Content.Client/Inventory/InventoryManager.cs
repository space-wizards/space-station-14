using Content.Client.Hands;
using Content.Client.Items.Managers;
using Content.Client.UserInterface;
using Content.Client.UserInterface.Controls;
using Content.Shared.Inventory;
using OpenToolkit.Graphics.OpenGL;
using Robust.Client.GameStates;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using Serilog;

namespace Content.Client.Inventory;

public sealed class InventoryUIController : UIController
{
    [Dependency] private IEntityManager _entityManager = default!;
    [UISystemDependency] private ClientInventorySystem? _inventorySystem = null;
    private Dictionary<string, ItemSlotUIContainer> _slotGroups = new();
    private ClientInventoryComponent? _playerInventoryComponent;
    public ClientInventoryComponent? PlayerInventory => _playerInventoryComponent;
    public override void OnGamestateChanged(GameStateAppliedArgs args)
    {
        DebugTest();
        if (_inventorySystem == null) return;
    }

    internal void SetPlayerInvComponent(ClientInventoryComponent? clientInv)
    {
        _playerInventoryComponent = clientInv;
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
