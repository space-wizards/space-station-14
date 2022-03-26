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
    [UISystemDependency] private InventorySystem? _inventorySystem = null;
    private Dictionary<string, ItemSlotUIContainer> _slotGroups = new();

    public override void OnGamestateChanged(GameStateAppliedArgs args)
    {
        DebugTest();
        if (_inventorySystem == null) return;
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
