using Content.Client.UserInterface.Controls;
using Content.Client.VendingMachines.UI;
using Content.Shared.VendingMachines;
using Robust.Client.UserInterface;
using Robust.Shared.Input;

namespace Content.Client.VendingMachines;

public sealed partial class VendingMachineBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
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
        var system = EntMan.System<VendingMachineSystem>();
        var inventory = system.GetAllInventory(Owner);

        _menu?.Populate(inventory, system.GetInventoryCategories(Owner), system.IsUiEnabled(Owner));
    }

    public void UpdateAmounts()
    {
        var system = EntMan.System<VendingMachineSystem>();
        var inventory = system.GetAllInventory(Owner);

        _menu?.UpdateAmounts(inventory, system.IsUiEnabled(Owner));
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
