using Content.Client.Gameplay;
using Content.Client.Info;
using Content.Client.UserInterface.Controls;
using Content.Shared.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input.Binding;
using MenuBar = Content.Client.UserInterface.Widgets.MenuBar;

namespace Content.Client.UserInterface.Controllers;

public sealed class InfoUIController : UIController, IOnStateChanged<GameplayState>
{
    private RulesAndInfoWindow? _window;
    private MenuButton InfoButton => UIManager.GetActiveUIWidget<MenuBar>().InfoButton;

    public void OnStateChanged(GameplayState state)
    {
        InfoButton.OnPressed += InfoButtonPressed;

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenInfo,
                InputCmdHandler.FromDelegate(_ => ToggleWindow()))
            .Register<CharacterUIController>();
    }

    private void InfoButtonPressed(BaseButton.ButtonEventArgs args)
    {
        ToggleWindow();
    }

    private void CreateWindow()
    {
        _window = UIManager.CreateNamedWindow<RulesAndInfoWindow>("RulesAndInfo");

        if (_window == null)
            return;

        _window.OpenCentered();
        InfoButton.Pressed = true;
    }

    private void CloseWindow()
    {
        if (_window == null)
            return;

        _window.Dispose();
        _window = null;
        InfoButton.Pressed = false;
    }

    private void ToggleWindow()
    {
        if (_window == null)
        {
            CreateWindow();
            return;
        }

        CloseWindow();
    }
}
