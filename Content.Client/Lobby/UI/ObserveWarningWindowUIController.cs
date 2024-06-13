using Robust.Client.UserInterface.Controllers;

namespace Content.Client.Lobby.UI;

public sealed class ObserveWarningWindowUIController : UIController
{
    private ObserveWarningWindow _observeWarningWindow = default!;

    private void EnsureWindow()
    {
        if (_observeWarningWindow is { Disposed: false })
            return;

        _observeWarningWindow = UIManager.CreateWindow<ObserveWarningWindow>();
    }

    public void OpenWindow()
    {
        EnsureWindow();

        _observeWarningWindow.OpenCentered();
        _observeWarningWindow.MoveToFront();
    }

    public void ToggleWindow()
    {
        EnsureWindow();

        if (_observeWarningWindow.IsOpen)
        {
            _observeWarningWindow.Close();
        }
        else
        {
            OpenWindow();
        }
    }
}

