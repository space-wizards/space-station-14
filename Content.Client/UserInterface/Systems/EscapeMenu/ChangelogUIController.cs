using Content.Client.Changelog;
using JetBrains.Annotations;
using Robust.Client.State;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.UserInterface.Systems.EscapeMenu;

[UsedImplicitly]
public sealed class ChangelogUIController : UIController
{
    [Dependency] private readonly IStateManager _stateManager = default!;

    private ChangelogWindow? _changeLogWindow = default!;

    public override void Initialize()
    {
        _stateManager.OnStateChanged += _ => CleanupWindow();
    }

    private void CleanupWindow()
    {
        _changeLogWindow?.DisposeAllChildren();
        _changeLogWindow = null;
    }

    public void OpenWindow()
    {
        _changeLogWindow ??= UIManager.CreateWindow<ChangelogWindow>();
        _changeLogWindow.OpenCentered();
        _changeLogWindow.MoveToFront();
    }

    public void ToggleWindow()
    {
        if (_changeLogWindow is { IsOpen: true })
        {
            _changeLogWindow.Close();
        }
        else
        {
            OpenWindow();
        }
    }
}
