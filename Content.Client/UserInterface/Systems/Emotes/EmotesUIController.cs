using Content.Client.Chat.UI;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input.Binding;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface.Systems.Emotes;

[UsedImplicitly]
public sealed class EmotesUIController : UIController, IOnStateChanged<GameplayState>
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IClyde _displayManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;

    private EmotesMenu? _menu;

    public void OnStateEntered(GameplayState state)
    {
        _menu = UIManager.CreateWindow<EmotesMenu>();

        var button = UIManager.GetActiveUIWidget<GameTopMenuBar>().EmotesButton;

        _menu.OnClose += () => button.SetClickPressed(false);
        _menu.OnOpen += () => button.SetClickPressed(true);
        _menu.OnPlayEmote += OnPlayEmote;

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenEmotesMenu,
                InputCmdHandler.FromDelegate(_ => ToggleWindow(false)))
            .Register<EmotesUIController>();
    }

    public void OnStateExited(GameplayState state)
    {
        _menu = null;

        CommandBinds.Unregister<EmotesUIController>();
    }

    public void ToggleWindow(bool centered)
    {
        if (_menu == null)
            return;

        if (!_menu.IsOpen)
        {
            if (centered)
                _menu.OpenCentered();

            else
            {
                // Open the menu, centered on the mouse
                var vpSize = _displayManager.ScreenSize;
                _menu.OpenCenteredAt(_inputManager.MouseScreenPosition.Position / vpSize);
            }
        }
        else
            _menu.Close();
    }

    private void OnPlayEmote(ProtoId<EmotePrototype> protoId)
    {
        _entityManager.RaisePredictiveEvent(new PlayEmoteMessage(protoId));
    }
}
