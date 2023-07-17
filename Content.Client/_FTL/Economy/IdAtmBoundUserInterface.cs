using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Fragments;
using Content.Shared._FTL.Economy;
using Content.Shared.CartridgeLoader;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client._FTL.Economy;

[UsedImplicitly]
public sealed class IdAtmBoundUserInterface : BoundUserInterface
{
    private PdaAtmUiWindow? _window;

    public IdAtmBoundUserInterface(EntityUid owner, Enum uiKey) : base (owner, uiKey) {}

    protected override void Open()
    {
        base.Open();

        _window = new PdaAtmUiWindow(this, Owner);

        _window.OpenCentered();
        _window.OnClose += Close;

        _window.OnWithdrawRequest += (amount) =>
        {
            SendIdAtmMessage(IdAtmUiAction.Withdrawal, amount);
        };
        _window.OnDepositRequest += () =>
        {
            SendIdAtmMessage(IdAtmUiAction.Deposit, 0);
        };
        _window.OnLockRequest += () =>
        {
            SendIdAtmPinMessage(IdAtmPinAction.Lock, "");
        };
        _window.OnUnlockRequest += (attempt) =>
        {
            SendIdAtmPinMessage(IdAtmPinAction.Unlock, attempt);
        };
        _window.OnPinChangeRequest += (newPin) =>
        {
            SendIdAtmPinMessage(IdAtmPinAction.Change, newPin);
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (_window == null)
            return;
        if (state is not IdAtmUiState pdaState)
        {
            return;
        }
        _window.SetMaxValueSlider(pdaState.Bank);
        _window.SetWelcomeMessage(pdaState.IdName, pdaState.Bank);
        _window.SetDisabledWithdrawButton(pdaState.Bank == 0);
        _window.SetDisabledDepositButton(pdaState.Cash == 0);

        var windowState = PdaAtmUiWindow.CurrentUIScreen.NoID;

        if (pdaState.IdCardIn)
        {
            windowState = pdaState.IdCardLocked
                ? PdaAtmUiWindow.CurrentUIScreen.Locked
                : PdaAtmUiWindow.CurrentUIScreen.Transaction;
        }

        _window.SetCurrentScreen(windowState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _window?.Dispose();
    }

    private void SendIdAtmPinMessage(IdAtmPinAction action, string attempt)
    {
        var message = new PinActionMessageEvent(action, attempt);
        SendMessage(message);
    }

    private void SendIdAtmMessage(IdAtmUiAction action, int amount)
    {
        if (action == IdAtmUiAction.Withdrawal)
        {
            amount = Math.Abs(amount);
            if (amount == 0)
                return;
        }
        var message = new IdAtmUiMessageEvent(Owner, action, amount);
        SendMessage(message);
    }
}
