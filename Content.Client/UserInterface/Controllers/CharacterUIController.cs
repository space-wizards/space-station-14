using Content.Client.CharacterInfo;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.UIWindows;
using Content.Shared.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.Input.Binding;
using static Content.Client.CharacterInfo.CharacterInfoSystem;
using static Robust.Client.UserInterface.Controls.BaseButton;
using MenuBar = Content.Client.UserInterface.Widgets.MenuBar;

namespace Content.Client.UserInterface.Controllers;

public sealed class CharacterUIController : UIController, IOnStateChanged<GameplayState>
{
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    [UISystemDependency] private readonly CharacterInfoSystem _characterInfo = default!;

    private CharacterWindow? _window;
    private MenuButton CharacterButton => UIManager.GetActiveUIWidget<MenuBar>().CharacterButton;

    public void OnStateChanged(GameplayState state)
    {
        CharacterButton.OnPressed += CharacterButtonPressed;

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenCharacterMenu,
                InputCmdHandler.FromDelegate(_ => ToggleWindow()))
            .Register<CharacterUIController>();
    }

    public override void OnSystemLoaded(IEntitySystem system)
    {
        switch (system)
        {
            case CharacterInfoSystem characterInterface:
                OnCharacterInfoSystemLoaded(characterInterface);
                break;
        }
    }

    public override void OnSystemUnloaded(IEntitySystem system)
    {
        switch (system)
        {
            case CharacterInfoSystem characterInterface:
                OnCharacterInfoSystemUnloaded(characterInterface);
                break;
        }
    }

    private void OnCharacterInfoSystemLoaded(CharacterInfoSystem system)
    {
        system.OnCharacterUpdate += CharacterUpdated;
        system.OnCharacterDetached += CharacterDetached;
    }

    private void OnCharacterInfoSystemUnloaded(CharacterInfoSystem system)
    {
        system.OnCharacterUpdate -= CharacterUpdated;
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

    private void CreateWindow()
    {
        _window = _ui.CreateNamedWindow<CharacterWindow>("Character");

        if (_window == null)
            return;

        _window.OpenCentered();
        _characterInfo.RequestCharacterInfo();
        CharacterButton.Pressed = true;
    }

    private void CloseWindow()
    {
        if (_window == null)
            return;

        _window.Dispose();
        _window = null;
        CharacterButton.Pressed = false;
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
