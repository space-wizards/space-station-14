// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Client.Backmen.Economy.ATM.UI;
using Content.Shared.Backmen.Economy.ATM;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Backmen.Economy.ATM;

public sealed class ATMBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private ATMMenu? _menu;

    public ATMBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {

    }
    protected override void Open()
    {
        base.Open();
        _menu = new ATMMenu { Title = IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(Owner).EntityName };

        _menu.IdCardButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent(AtmComponent.IdCardSlotId));
        _menu.OnWithdrawAttempt += OnWithdrawAttempt;

        _menu.OnClose += Close;
        _menu.OpenCentered();
    }

    private void OnWithdrawAttempt(LineEdit.LineEditEventArgs args, FixedPoint2 amount)
    {
        SendMessage(new ATMRequestWithdrawMessage(amount, args.Text));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_menu == null)
            return;

        if (state is AtmBoundUserInterfaceState cast)
        {
            _menu.UpdateState(cast);
        }
        else if (state is AtmBoundUserInterfaceBalanceState cast2)
        {
            _menu.UpdateBalanceState(cast2);
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        if (_menu == null)
            return;

        _menu.OnClose -= Close;
        _menu.Dispose();
    }
}
