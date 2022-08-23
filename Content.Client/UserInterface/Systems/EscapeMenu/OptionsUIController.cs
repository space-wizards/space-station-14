using Content.Client.Gameplay;
using Content.Client.Options.UI;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.EscapeMenu;

[UsedImplicitly]
public sealed class OptionsUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    private OptionsMenu? _optionsWindow;
    public void OnStateEntered(GameplayState state)
    {
        DebugTools.Assert(_optionsWindow == null);
        _optionsWindow = UIManager.CreateWindow<OptionsMenu>();
    }

    public void OnStateExited(GameplayState state)
    {
        _optionsWindow?.DisposeAllChildren();
        _optionsWindow = null;
    }
    public void OpenWindow()
    {
        _optionsWindow?.OpenCentered();
    }

    public void ToggleWindow()
    {
        if (_optionsWindow == null)
            return;

        if (_optionsWindow.IsOpen)
        {
            _optionsWindow?.Close();
        }
        else
        {
            _optionsWindow.OpenCentered();
        }
    }
}
