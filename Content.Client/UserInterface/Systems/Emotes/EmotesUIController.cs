using System.Linq;
using Content.Client.Chat.UI;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
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
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.Emotes;

[UsedImplicitly]
public sealed class EmotesUIController : UIController, IOnStateChanged<GameplayState>
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IClyde _displayManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private MenuButton? EmotesButton => UIManager.GetActiveUIWidgetOrNull<MenuBar.Widgets.GameTopMenuBar>()?.EmotesButton;
    private RadialMenu? _menu;

    private readonly Dictionary<EmoteCategory, (string Tooltip, SpriteSpecifier Sprite)> _dict = new Dictionary<EmoteCategory, (string Tooltip, SpriteSpecifier Sprite)>
    {
        [EmoteCategory.General] = ("emote-menu-category-general", new SpriteSpecifier.Texture(new ResPath("/Textures/Clothing/Head/Soft/mimesoft.rsi/icon.png"))),
        [EmoteCategory.Hands] = ("emote-menu-category-hands", new SpriteSpecifier.Texture(new ResPath("/Textures/Clothing/Hands/Gloves/latex.rsi/icon.png"))),
        [EmoteCategory.Vocal] = ("emote-menu-category-vocal", new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/Emotes/vocal.png"))),
    };

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
            // setup window
            var models = _prototypeManager.EnumeratePrototypes<EmotePrototype>()
                                          .GroupBy(x => x.Category)
                                          .Select(categoryGroup =>
                                          {
                                              var nestedEmotes = categoryGroup.Select(
                                                  emote => new RadialMenuButtonModel(() => _entityManager.RaisePredictiveEvent(new PlayEmoteMessage(emote.ID)))
                                                  {
                                                      Sprite = emote.Icon,
                                                      ToolTip = Loc.GetString(emote.Name)
                                                  }
                                              ).ToArray();
                                              var tuple = _dict[categoryGroup.Key];
                                              return new RadialMenuButtonModel(nestedEmotes)
                                              {
                                                  Sprite = tuple.Sprite,
                                                  ToolTip = Loc.GetString(tuple.Tooltip)
                                              };
                                          });


            _menu = new SimpleRadialMenu(models);
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
                // Open the menu, centered on the mouse
                var vpSize = _displayManager.ScreenSize;
                _menu.OpenCenteredAt(_inputManager.MouseScreenPosition.Position / vpSize);
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
}
