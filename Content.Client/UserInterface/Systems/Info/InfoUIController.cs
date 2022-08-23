using Content.Client.Administration.Systems;
using Content.Client.Gameplay;
using Content.Client.Info;
using Content.Client.UserInterface.Controls;
using Content.Shared.Input;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input.Binding;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.Info;

public sealed class InfoUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>, IOnSystemChanged<BwoinkSystem>
{
    private RulesAndInfoWindow? _infoWindow;
    private MenuButton? _infoButton;

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
        _infoButton?.StyleClasses.Add(MenuButton.StyleClassRedTopButton);
    }

    private void AdminOpenedAHelp()
    {
        _infoButton?.StyleClasses.Remove(MenuButton.StyleClassRedTopButton);
    }

    public void OnStateEntered(GameplayState state)
    {
        DebugTools.Assert(_infoWindow == null);
        _infoButton = UIManager.GetActiveUIWidget<MenuBar.Widgets.MenuBar>().InfoButton;
        _infoButton.OnPressed += InfoButtonPressed;

        _infoWindow = UIManager.CreateWindow<RulesAndInfoWindow>();
        _infoWindow.OnClose += () => { _infoButton.Pressed = false; };
        _infoWindow.OnOpen +=  () => { _infoButton.Pressed = true; };


        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenInfo,
                InputCmdHandler.FromDelegate(_ => ToggleWindow()))
            .Register<InfoUIController>();
    }

    public void OnStateExited(GameplayState state)
    {
        _infoWindow?.DisposeAllChildren();
        _infoWindow = null;
        CommandBinds.Unregister<InfoUIController>();
        if (_infoButton == null)
            return;
        _infoButton.OnPressed -= InfoButtonPressed;
    }

    private void InfoButtonPressed(BaseButton.ButtonEventArgs args)
    {
        ToggleWindow();
    }

    public void OpenWindow()
    {
        _infoWindow?.OpenCentered();
    }

    private void ToggleWindow()
    {
        if (_infoWindow == null)
            return;
        if (_infoWindow.IsOpen)
        {
            _infoWindow.Close();
        }
        else
        {
            _infoWindow?.OpenCentered();
        }
    }
}
