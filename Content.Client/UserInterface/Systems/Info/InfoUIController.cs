using Content.Client.Administration.Systems;
using Content.Client.Gameplay;
using Content.Client.Info;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Character;
using Content.Shared.Input;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input.Binding;

namespace Content.Client.UserInterface.Systems.Info;

public sealed class InfoUIController : UIController, IOnStateEntered<GameplayState>, IOnSystemChanged<BwoinkSystem>
{
    private RulesAndInfoWindow? _window;
    private MenuButton InfoButton => UIManager.GetActiveUIWidget<MenuBar.Widgets.MenuBar>().InfoButton;

    public void OnSystemLoaded(BwoinkSystem system)
    {
        system.AdminReceivedAHelp += AdminReceivedAHelp;
        system.AdminOpenedAHelp += AdminOpenedAHelp;
    }

    public void OnSystemUnloaded(BwoinkSystem system)
    {
        system.AdminReceivedAHelp -= AdminReceivedAHelp;
        system.AdminOpenedAHelp -= AdminOpenedAHelp;
    }

    private void AdminReceivedAHelp()
    {
        SetInfoRed(true);
    }

    private void AdminOpenedAHelp()
    {
        SetInfoRed(false);
    }

    public void OnStateEntered(GameplayState state)
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
        _window = UIManager.CreateWindow<RulesAndInfoWindow>();

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

    public void SetInfoRed(bool value)
    {
        if (value)
            InfoButton.StyleClasses.Add(MenuButton.StyleClassRedTopButton);
        else
            InfoButton.StyleClasses.Remove(MenuButton.StyleClassRedTopButton);
    }
}
