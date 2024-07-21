using Content.Shared.Bed.Cryostorage;
using JetBrains.Annotations;

namespace Content.Client.Bed.Cryostorage;

[UsedImplicitly]
public sealed class CryostorageBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private CryostorageMenu? _menu;

    public CryostorageBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = new();

        _menu.OnClose += Close;

        _menu.SlotRemoveButtonPressed += (ent, slot) =>
        {
            SendMessage(new CryostorageRemoveItemBuiMessage(ent, slot, CryostorageRemoveItemBuiMessage.RemovalType.Inventory));
        };

        _menu.HandRemoveButtonPressed += (ent, hand) =>
        {
            SendMessage(new CryostorageRemoveItemBuiMessage(ent, hand, CryostorageRemoveItemBuiMessage.RemovalType.Hand));
        };

        _menu.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        switch (state)
        {
            case CryostorageBuiState msg:
                _menu?.UpdateState(msg);
                break;
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;
        _menu?.Dispose();
    }
}
