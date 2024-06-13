using Robust.Client.UserInterface.Controllers;

namespace Content.Client.Lobby.UI;

public sealed class ObserveWarningWindowUIController : UIController
{
    private ObserveWarningWindow _optionsWindow = default!;

    private void EnsureWindow()
    {
        if (_optionsWindow is { Disposed: false })
            return;

        _optionsWindow = UIManager.CreateWindow<ObserveWarningWindow>();
    }

    public void OpenWindow()
    {
        EnsureWindow();

        _optionsWindow.OpenCentered();
        _optionsWindow.MoveToFront();
    }

    public void ToggleWindow()
    {
        EnsureWindow();

        if (_optionsWindow.IsOpen)
        {
            _optionsWindow.Close();
        }
        else
        {
            OpenWindow();
        }
    }
}

