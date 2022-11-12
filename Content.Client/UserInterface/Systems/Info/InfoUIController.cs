using Content.Client.Gameplay;
using Content.Client.Info;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.UserInterface.Systems.Info;

public sealed class InfoUIController : UIController, IOnStateExited<GameplayState>
{
    private RulesAndInfoWindow? _infoWindow;

    public void OnStateExited(GameplayState state)
    {
        if (_infoWindow == null)
            return;

        _infoWindow.Dispose();
        _infoWindow = null;
    }

    public void OpenWindow()
    {
        if (_infoWindow == null || _infoWindow.Disposed)
        {
            _infoWindow = UIManager.CreateWindow<RulesAndInfoWindow>();
        }

        _infoWindow?.OpenCentered();
    }
}
