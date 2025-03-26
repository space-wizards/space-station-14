using Content.Client.Chat.UI;
using Content.Client.Gameplay;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface.Controllers;
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
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenEmotesMenu,
                InputCmdHandler.FromDelegate(_ => ToggleWindow(false)))
            .Register<EmotesUIController>();
    }

    public void OnStateExited(GameplayState state)
    {
        CommandBinds.Unregister<EmotesUIController>();
    }

    public void ToggleWindow(bool centered)
    {
        if (_menu == null)
        {
            _menu = UIManager.CreateWindow<EmotesMenu>();
            _menu.OnPlayEmote += OnPlayEmote;

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
        {
            _menu.Close();

            _menu = null;
        }
    }

    private void OnPlayEmote(ProtoId<EmotePrototype> protoId)
    {
        _entityManager.RaisePredictiveEvent(new PlayEmoteMessage(protoId));
    }
}
