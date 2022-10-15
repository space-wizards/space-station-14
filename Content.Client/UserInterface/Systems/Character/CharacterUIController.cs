using Content.Client.CharacterInfo;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Character.Controls;
using Content.Client.UserInterface.Systems.Character.Windows;
using Content.Client.UserInterface.Systems.Objectives.Controls;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.Input.Binding;
using Robust.Shared.Utility;
using static Content.Client.CharacterInfo.CharacterInfoSystem;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.UserInterface.Systems.Character;

[UsedImplicitly]
public sealed class CharacterUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>, IOnSystemChanged<CharacterInfoSystem>
{
    [UISystemDependency] private readonly CharacterInfoSystem _characterInfo = default!;

    private CharacterWindow? _window;
    private MenuButton? _characterButton;

    public void OnStateEntered(GameplayState state)
    {
        DebugTools.Assert(_window == null);
        _characterButton = UIManager.GetActiveUIWidget<MenuBar.Widgets.GameTopMenuBar>().CharacterButton;
        _characterButton.OnPressed += CharacterButtonPressed;

        _window = UIManager.CreateWindow<CharacterWindow>();
        LayoutContainer.SetAnchorPreset(_window, LayoutContainer.LayoutPreset.CenterTop);

        _window.OnClose += () => { _characterButton.Pressed = false; };
        _window.OnOpen += () => { _characterButton.Pressed = true; };

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenCharacterMenu,
                 InputCmdHandler.FromDelegate(_ => ToggleWindow()))
             .Register<CharacterUIController>();
    }

    public void OnStateExited(GameplayState state)
    {
        if (_window != null)
        {
            _window.Dispose();
            _window = null;
        }

        if (_characterButton != null)
        {
            _characterButton.OnPressed -= CharacterButtonPressed;
            _characterButton.Pressed = false;
            _characterButton = null;
        }

        CommandBinds.Unregister<CharacterUIController>();
    }

    public void OnSystemLoaded(CharacterInfoSystem system)
    {
        system.OnCharacterUpdate += CharacterUpdated;
        system.OnCharacterDetached += CharacterDetached;
    }

    public void OnSystemUnloaded(CharacterInfoSystem system)
    {
        system.OnCharacterUpdate -= CharacterUpdated;
        system.OnCharacterDetached -= CharacterDetached;
    }

    private void CharacterUpdated(CharacterData data)
    {
        if (_window == null)
        {
            return;
        }

        var (job, objectives, briefing, sprite, entityName) = data;

        _window.SubText.Text = job;
        _window.Objectives.RemoveAllChildren();

        foreach (var (groupId, conditions) in objectives)
        {
            var objectiveControl = new CharacterObjectiveControl
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                Modulate = Color.Gray
            };

            objectiveControl.AddChild(new Label
            {
                Text = groupId,
                Modulate = Color.LightSkyBlue
            });

            foreach (var condition in conditions)
            {
                var conditionControl = new ObjectiveConditionsControl();
                conditionControl.ProgressTexture.Texture = condition.SpriteSpecifier.Frame0();
                conditionControl.ProgressTexture.Progress = condition.Progress;

                conditionControl.Title.Text = condition.Title;
                conditionControl.Description.Text = condition.Description;

                objectiveControl.AddChild(conditionControl);
            }

            var briefingControl = new ObjectiveBriefingControl();
            briefingControl.Label.Text = briefing;

            objectiveControl.AddChild(briefingControl);
            _window.Objectives.AddChild(objectiveControl);
        }

        _window.SpriteView.Sprite = sprite;
        _window.NameLabel.Text = entityName;
    }

    private void CharacterDetached()
    {
        CloseWindow();
    }

    private void CharacterButtonPressed(ButtonEventArgs args)
    {
        ToggleWindow();
    }

    private void CloseWindow()
    {
        _window!.Close();
    }

    private void ToggleWindow()
    {
        if (_window == null)
            return;
        if (_window.IsOpen)
        {
            CloseWindow();
        }
        else
        {
            _characterInfo.RequestCharacterInfo();
            _window.Open();
        }
    }
}
