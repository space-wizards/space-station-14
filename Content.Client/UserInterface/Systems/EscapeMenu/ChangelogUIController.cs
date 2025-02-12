using Content.Client.Changelog;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.UserInterface.Systems.EscapeMenu;

[UsedImplicitly]
public sealed class ChangelogUIController : UIController
{
    private ChangelogWindow _changeLogWindow = default!;

    public void OpenWindow()
    {
        EnsureWindow();

        _changeLogWindow.OpenCentered();
        _changeLogWindow.MoveToFront();
    }

    private void EnsureWindow()
    {
        if (_changeLogWindow is { Disposed: false })
            return;

        _changeLogWindow = UIManager.CreateWindow<ChangelogWindow>();
    }

    public void ToggleWindow()
    {
        EnsureWindow();

        if (_changeLogWindow.IsOpen)
        {
            _changeLogWindow.Close();
        }
        else
        {
            OpenWindow();
        }
    }
}
