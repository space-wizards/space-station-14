using Content.Client.Options.UI;
using JetBrains.Annotations;
using Robust.Client.State;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.UserInterface.Systems.EscapeMenu;

[UsedImplicitly]
public sealed class OptionsUIController : UIController
{
    [Dependency] private readonly IStateManager _stateManager = default!;

    private OptionsMenu _optionsWindow = default!;

    public override void Initialize()
    {
        _stateManager.OnStateChanged += _ => ReloadWindow();
        _optionsWindow = UIManager.CreateWindow<OptionsMenu>();
    }

    private void ReloadWindow()
    {
        _optionsWindow.DisposeAllChildren();
        _optionsWindow = UIManager.CreateWindow<OptionsMenu>();
    }

    public void OpenWindow()
    {
        _optionsWindow.OpenCentered();
        _optionsWindow.MoveToFront();
    }

    public void ToggleWindow()
    {
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
