using Robust.Shared.Console;
using Content.Client.Administration.UI.ShutdownConfirm;

namespace Content.Client.Administration.Commands;

public sealed class ShutdownCommand : IConsoleCommand
{
    public string Command => "shutdown";
    public string Description => "Shuts down the server after confirmation.";
    public string Help => "Usage: shutdown";

    private ShutdownConfirmationWindow? _confirmationWindow;

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_confirmationWindow != null && _confirmationWindow.IsOpen)
        {
            return;
        }

        _confirmationWindow = new ShutdownConfirmationWindow();
        _confirmationWindow.ConfirmButton.OnPressed += _ =>
        {
            shell.RemoteExecuteCommand("shutdownserver");
            _confirmationWindow.Close();
        };

        _confirmationWindow.CancelButton.OnPressed += _ =>
        {
            _confirmationWindow.Close();
        };

        _confirmationWindow.OpenCentered();
    }
}

