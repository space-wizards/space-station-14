using Content.Client.Actions;
using Content.Client.CF.Hud;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Actions;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Input;
using Content.Shared.Whitelist;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input.Binding;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface.Systems.Emotes;

[UsedImplicitly]
public sealed partial class EmotesUIController : UIController, IOnStateChanged<GameplayState>
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;

    private MenuButton? EmotesButton => UIManager.GetActiveUIWidgetOrNull<MenuBar.Widgets.GameTopMenuBar>()?.EmotesButton;
    private SimpleRadialMenu? _menu;

    public void OnStateEntered(GameplayState state)
    {
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenEmotesMenu,
                InputCmdHandler.FromDelegate(_ => ToggleEmotesMenu(false)))
            .Register<EmotesUIController>();
    }

    public void OnStateExited(GameplayState state)
    {
        CommandBinds.Unregister<EmotesUIController>();
    }

    private void ToggleEmotesMenu(bool centered)
    {
        if (_menu == null)
        {
            // CarpFinder: vocal chat stays. The other slices are actions, reactions, and spell ranks.
            _menu = new SimpleRadialMenu();
            var models = CFRadialMenu.Build(
                _menu,
                _prototypeManager.EnumeratePrototypes<EmotePrototype>(),
                _playerManager.LocalSession?.AttachedEntity,
                EntitySystemManager.GetEntitySystem<EntityWhitelistSystem>(),
                EntityManager,
                EntitySystemManager.GetEntitySystem<ActionsSystem>(),
                HandleRadialButtonClick,
                action => UIManager.GetUIController<ActionUIController>().UseAction(action));
            _menu.SetButtons(models);

            _menu.Open();

            _menu.OnClose += OnWindowClosed;
            _menu.OnOpen += OnWindowOpen;

            if (EmotesButton != null)
                EmotesButton.SetClickPressed(true);

            if (centered)
            {
                _menu.OpenCentered();
            }
            else
            {
                _menu.OpenOverMouseScreenPosition();
            }
        }
        else
        {
            _menu.OnClose -= OnWindowClosed;
            _menu.OnOpen -= OnWindowOpen;

            if (EmotesButton != null)
                EmotesButton.SetClickPressed(false);

            CloseMenu();
        }
    }

    public void UnloadButton()
    {
        if (EmotesButton == null)
            return;

        EmotesButton.OnPressed -= ActionButtonPressed;
    }

    public void LoadButton()
    {
        if (EmotesButton == null)
            return;

        EmotesButton.OnPressed += ActionButtonPressed;
    }

    private void ActionButtonPressed(BaseButton.ButtonEventArgs args)
    {
        ToggleEmotesMenu(true);
    }

    private void OnWindowClosed()
    {
        if (EmotesButton != null)
            EmotesButton.Pressed = false;

        CloseMenu();
    }

    private void OnWindowOpen()
    {
        if (EmotesButton != null)
            EmotesButton.Pressed = true;
    }

    private void CloseMenu()
    {
        if (_menu == null)
            return;

        _menu.Dispose();
        _menu = null;
    }

    private void HandleRadialButtonClick(EmotePrototype prototype)
    {
        EntityManager.RaisePredictiveEvent(new PlayEmoteMessage(prototype.ID));
    }
}
