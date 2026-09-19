using Content.Client.UserInterface.Controls;
using Content.Client.VendingMachines.UI;
using Content.Shared.VendingMachines;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Prototypes;
using Content.Shared.VendingMachines.Components;

namespace Content.Client.VendingMachines;

public sealed partial class VendingMachineBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    [ViewVariables]
    private VendingMachineMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindowCenteredLeft<VendingMachineMenu>();
        _menu.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
        _menu.OnItemSelected += OnItemSelected;
        Refresh();
    }

    public void Refresh()
    {
        var enabled = EntMan.TryGetComponent(Owner, out VendingMachineEjectComponent? eject) && !eject.Ejecting;

        var system = EntMan.System<VendingMachineSystem>();
        var inventory = system.GetAllInventory(Owner);

        IReadOnlyList<VendingMachineInventoryCategory> categories = [];
        if (EntMan.TryGetComponent(Owner, out VendingMachineComponent? vending) &&
            _prototypeManager.Resolve(vending.PackPrototypeId, out VendingMachineInventoryPrototype? inventoryPrototype))
        {
            categories = inventoryPrototype.Categories;
        }

        _menu?.Populate(inventory, categories, enabled);
    }

    public void UpdateAmounts()
    {
        var enabled = EntMan.TryGetComponent(Owner, out VendingMachineEjectComponent? eject) && !eject.Ejecting;

        var system = EntMan.System<VendingMachineSystem>();
        var inventory = system.GetAllInventory(Owner);
        _menu?.UpdateAmounts(inventory, enabled);
    }

    private void OnItemSelected(GUIBoundKeyEventArgs args, ListData data)
    {
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        if (data is not VendorItemsListData { ItemType: var type, ItemProtoID: var id })
            return;

        SendPredictedMessage(new VendingMachineEjectMessage(type, id));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        if (_menu == null)
            return;

        _menu.OnItemSelected -= OnItemSelected;
        _menu.OnClose -= Close;
        _menu.Dispose();
    }
}
