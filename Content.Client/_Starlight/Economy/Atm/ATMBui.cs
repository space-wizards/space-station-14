using Content.Client._Starlight.Antags.Abductor;
using Content.Shared.Starlight.Antags.Abductor;
using Content.Shared.Starlight.Economy.Atm;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;
using static Content.Shared.Pinpointer.SharedNavMapSystem;

namespace Content.Client._Starlight.Economy.Atm;

[UsedImplicitly]
public sealed class ATMBui : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entities = default!;

    [ViewVariables]
    private ATMWindow? _window;
    private int _amount;
    public ATMBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {

    }
    protected override void Open()
    {
        base.Open();
        UpdateState(State);
    }

    protected override void UpdateState(BoundUserInterfaceState? state)
    {
        if (state is ATMBuiState s)
            Update(s);
    }

    private void Update(ATMBuiState state)
    {
        TryInitWindow();

        View(ViewType.Withdraw);

        RefreshUI();

        if (!_window!.IsOpen)
            _window.OpenCentered();
    }

    private void TryInitWindow()
    {
        if (_window != null) return;
        _window = new ATMWindow();
        _window.OnClose += Close;
        _window.Title = "automated teller machine";

        _window.WithdrawTabButton.OnPressed += _ => View(ViewType.Withdraw);

        _window.TransferTabButton.OnPressed += _ => View(ViewType.Transfer);
    }

    private void RefreshUI()
    {
        if (_window == null || State is not ATMBuiState state)
            return;

        _window.BalanceLabel.Children.Clear();

        var balanceMsg = new FormattedMessage();
        balanceMsg.AddMarkupOrThrow(Loc.GetString("economy-atm-ui-balance", ("balance", state.Balance)));
        _window.BalanceLabel.SetMessage(balanceMsg);

        _window.WithdrawInput.OnTextChanged += _ =>
        {
            _window.WithdrawButton.Disabled = !int.TryParse(_window.WithdrawInput.Text, out var amount) || amount <= 0 || amount > state.Balance;
            _amount = amount;
        };
        _window.WithdrawButton.OnPressed += _ =>
        {
            SendMessage(new ATMWithdrawBuiMsg() { Amount = _amount });
            Close();
        };
    }

    private void View(ViewType type)
    {
        if (_window == null)
            return;

        _window.WithdrawTabButton.Parent!.Margin = new Thickness(0, 0, 0, 10);

        _window.WithdrawTabButton.Disabled = type == ViewType.Withdraw;
        _window.TransferTabButton.Disabled = type == ViewType.Transfer;
        _window.WithdrawTab.Visible = type == ViewType.Withdraw;
        _window.TransferTab.Visible = type == ViewType.Transfer;
    }

    private enum ViewType
    {
        Withdraw,
        Transfer
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _window?.Dispose();
    }
}
