using JetBrains.Annotations;
using Content.Client._Starlight.Language;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Shared.Input;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input.Binding;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BaseButton;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;

namespace Content.Client._Starlight.UserInterface.Systems.Language;

[UsedImplicitly]
public sealed class LanguageMenuUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    public LanguageMenuWindow? LanguageWindow;
    private MenuButton? LanguageButton => UIManager.GetActiveUIWidgetOrNull<GameTopMenuBar>()?.LanguageButton;

    public void OnStateEntered(GameplayState state)
    {
        DebugTools.Assert(LanguageWindow == null);

        LanguageWindow = UIManager.CreateWindow<LanguageMenuWindow>();
        LayoutContainer.SetAnchorPreset(LanguageWindow, LayoutContainer.LayoutPreset.CenterTop);

        LanguageWindow.OnClose += () =>
        {
            if (LanguageButton != null)
                LanguageButton.Pressed = false;
        };
        LanguageWindow.OnOpen += () =>
        {
            if (LanguageButton != null)
                LanguageButton.Pressed = true;
        };

        CommandBinds.Builder.Bind(ContentKeyFunctions.OpenLanguageMenu,
            InputCmdHandler.FromDelegate(_ => ToggleWindow())).Register<LanguageMenuUIController>();
    }

    public void OnStateExited(GameplayState state)
    {
        if (LanguageWindow != null)
        {
            LanguageWindow.Dispose();
            LanguageWindow = null;
        }

        CommandBinds.Unregister<LanguageMenuUIController>();
    }

    public void UnloadButton()
    {
        if (LanguageButton == null)
            return;

        LanguageButton.OnPressed -= LanguageButtonPressed;
    }

    public void LoadButton()
    {
        if (LanguageButton == null)
            return;

        LanguageButton.OnPressed += LanguageButtonPressed;
    }

    private void LanguageButtonPressed(ButtonEventArgs args)
    {
        ToggleWindow();
    }

    private void ToggleWindow()
    {
        if (LanguageWindow == null)
            return;

        if (LanguageButton != null)
            LanguageButton.SetClickPressed(!LanguageWindow.IsOpen);

        if (LanguageWindow.IsOpen)
            LanguageWindow.Close();
        else
            LanguageWindow.Open();
    }
}