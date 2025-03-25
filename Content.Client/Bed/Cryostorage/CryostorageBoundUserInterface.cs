using Content.Shared.Bed.Cryostorage;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

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

        _menu = this.CreateWindow<CryostorageMenu>();

        _menu.SlotRemoveButtonPressed += (ent, slot) =>
        {
            SendMessage(new CryostorageRemoveItemBuiMessage(ent, slot, CryostorageRemoveItemBuiMessage.RemovalType.Inventory));
        };

        _menu.HandRemoveButtonPressed += (ent, hand) =>
        {
            SendMessage(new CryostorageRemoveItemBuiMessage(ent, hand, CryostorageRemoveItemBuiMessage.RemovalType.Hand));
        };
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
}
