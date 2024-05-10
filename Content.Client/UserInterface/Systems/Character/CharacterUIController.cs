using System.Numerics;
using Content.Client.Gameplay;
using Content.Client.Message;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Character.Controls;
using Content.Client.UserInterface.Systems.Character.Windows;
using Content.Client.UserInterface.Systems.Objectives.Controls;
using Content.Shared.Input;
using Content.Shared.Mind;
using Content.Shared.Objectives;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input.Binding;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.UserInterface.Systems.Character;

[UsedImplicitly]
public sealed class CharacterUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    [UISystemDependency] private readonly SpriteSystem _sprite = default!;
    [UISystemDependency] private readonly SharedMindSystem _minds = default!;
    [UISystemDependency] private readonly RoleSystem _roles = default!;
    [UISystemDependency] private readonly SharedObjectivesSystem _objectives = default!;
    [UISystemDependency] private readonly SharedJobSystem _jobs = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IEntityManager _entity = default!;

    private CharacterWindow? _window;
    private MenuButton? CharacterButton => UIManager.GetActiveUIWidgetOrNull<MenuBar.Widgets.GameTopMenuBar>()?.CharacterButton;

    public void OnStateEntered(GameplayState state)
    {
        DebugTools.Assert(_window == null);

        _window = UIManager.CreateWindow<CharacterWindow>();
        LayoutContainer.SetAnchorPreset(_window, LayoutContainer.LayoutPreset.CenterTop);

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

        CommandBinds.Unregister<CharacterUIController>();
    }

    public void UnloadButton()
    {
        if (CharacterButton == null)
        {
            return;
        }

        CharacterButton.OnPressed -= CharacterButtonPressed;
    }

    public void LoadButton()
    {
        if (CharacterButton == null)
        {
            return;
        }

        CharacterButton.OnPressed += CharacterButtonPressed;

        if (_window == null)
        {
            return;
        }

        _window.OnClose += DeactivateButton;
        _window.OnOpen += ActivateButton;
    }

    private void DeactivateButton() => CharacterButton!.Pressed = false;
    private void ActivateButton() => CharacterButton!.Pressed = true;

    public void UpdateCharacterWindow()
    {
        if (_window == null)
            return;

        if (_player.LocalEntity is not { } entity)
            return;

        var entityName = _entity.GetComponent<MetaDataComponent>(entity).EntityName;

        _window.SpriteView.SetEntity(entity);
        _window.NameLabel.SetMarkup(Loc.GetString("character-info-name-label", ("name", FormattedMessage.EscapeText(entityName))));

        // all further info requires a mind more or less
        if (!_minds.TryGetMind(entity, out var mindId, out var mind))
            return;

        if (_jobs.MindTryGetJobName(mindId, out var jobName))
            _window.SubTextLabel.SetMarkup(Loc.GetString("character-info-subtext-label", ("job", FormattedMessage.EscapeText(jobName))));

        // Get briefing
        var briefing = _roles.MindGetBriefing(mindId);

        // Get all objectives
        _window.ObjectivesContainer.RemoveAllChildren();

        if (mind.Objectives.Count == 0)
        {
            _window.ObjectivesPanel.MinSize = Vector2.Zero;
            _window.ObjectivesPanel.SetSize = Vector2.Zero;
        }

        var objectivesSorted = new Dictionary<string, List<ObjectiveInfo>>();

        foreach (var objective in mind.Objectives)
        {
            var info = _objectives.GetInfo(objective, mindId, mind);

            if (info == null)
                continue;

            // group objectives by their issuer
            var issuer = _entity.GetComponent<ObjectiveComponent>(objective).Issuer;

            if (!objectivesSorted.ContainsKey(issuer))
                objectivesSorted[issuer] = new();
            objectivesSorted[issuer].Add(info.Value);
        }

        foreach (var (groupId, conditions) in objectivesSorted)
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
                conditionControl.ProgressTexture.Texture = _sprite.Frame0(condition.Icon);
                conditionControl.ProgressTexture.Progress = condition.Progress;
                var titleMessage = new FormattedMessage();
                var descriptionMessage = new FormattedMessage();
                titleMessage.AddText(condition.Title);
                descriptionMessage.AddText(condition.Description);

                conditionControl.Title.SetMessage(titleMessage);
                conditionControl.Description.SetMessage(descriptionMessage);

                objectiveControl.AddChild(conditionControl);
            }

            _window.ObjectivesContainer.AddChild(objectiveControl);
        }

        if (briefing != null)
        {
            var briefingControl = new ObjectiveBriefingControl();
            var text = new FormattedMessage();
            text.PushColor(Color.Yellow);
            text.AddText(briefing);
            briefingControl.Label.SetMessage(text);
            _window.BriefingContainer.AddChild(briefingControl);
        }

        var controls = GetCharacterInfoControls(entity);
        foreach (var control in controls)
        {
            _window.BriefingContainer.AddChild(control);
        }
    }

    public List<Control> GetCharacterInfoControls(EntityUid uid)
    {
        var ev = new GetCharacterInfoControlsEvent(uid);
        _entity.EventBus.RaiseLocalEvent(uid, ref ev, true);
        return ev.Controls;
    }

    // TODO MIRROR?
    private void CharacterDetached(EntityUid uid)
    {
        CloseWindow();
    }

    private void CharacterButtonPressed(ButtonEventArgs args)
    {
        ToggleWindow();
    }

    private void CloseWindow()
    {
        _window?.Close();
    }

    private void ToggleWindow()
    {
        if (_window == null)
            return;

        if (CharacterButton != null)
        {
            CharacterButton.SetClickPressed(!_window.IsOpen);
        }

        if (_window.IsOpen)
        {
            CloseWindow();
        }
        else
        {
            UpdateCharacterWindow();
            _window.Open();
        }
    }
}

/// <summary>
/// Event raised to get additional controls to display in the character info menu.
/// </summary>
[ByRefEvent]
public readonly record struct GetCharacterInfoControlsEvent(EntityUid Entity)
{
    public readonly List<Control> Controls = new();

    public readonly EntityUid Entity = Entity;
}
