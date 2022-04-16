using Content.Client.Gameplay;
using Content.Client.HUD;
using Content.Client.Info;
using Content.Client.UserInterface.Controls;
using Content.Shared.Input;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input.Binding;
using MenuBar = Content.Client.HUD.Widgets.MenuBar;

namespace Content.Client.UserInterface.Controllers;

public sealed class InfoUIController : UIController
{
    [Dependency] private readonly IHudManager _hud = default!;
    [Dependency] private readonly IUIWindowManager _uiWindows = default!;

    private RulesAndInfoWindow? _window;
    private MenuButton InfoButton => _hud.GetUIWidget<MenuBar>().InfoButton;

    public override void OnStateChanged(StateChangedEventArgs args)
    {
        if (args.NewState is GameplayState)
        {
            InfoButton.OnPressed += InfoButtonPressed;

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.OpenInfo,
                    InputCmdHandler.FromDelegate(_ => ToggleWindow()))
                .Register<CharacterUIController>();
        }
    }

    private void InfoButtonPressed(BaseButton.ButtonEventArgs args)
    {
        ToggleWindow();
    }

    private void CreateWindow()
    {
        _window = _uiWindows.CreateNamedWindow<RulesAndInfoWindow>("RulesAndInfo");

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
