// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.Decals.UI;
using Content.Client.Gameplay;
using Content.Client.Sandbox;
using Content.Shared.Decals;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.SS220.GhostRoleCast;

public sealed class GhostRoleCastUIController : UIController
{
    private GhostRoleCastWindow? _window;

    public void ToggleWindow()
    {
        EnsureWindow();

        if (_window!.IsOpen)
        {
            _window.Close();
        }
        else
        {
            _window.Open();
        }
    }

    public void OnStateExited(GameplayState state)
    {
        if (_window == null)
            return;
        _window.Dispose();
        _window = null;
    }

    private void EnsureWindow()
    {
        if (_window is { Disposed: false })
            return;
        _window = UIManager.CreateWindow<GhostRoleCastWindow>();
    }
}
