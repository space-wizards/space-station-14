using Content.Client.Actions;
using Content.Client.Gameplay;
using Content.Client.HUD;
using Content.Client.HUD.Widgets;
using Content.Client.Outline;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.UIWindows;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Input;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared.Input.Binding;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.UserInterface.Controllers;

public sealed class ActionUIController : UIController
{
    [Dependency] private readonly IHudManager _hud = default!;
    [Dependency] private readonly IUIWindowManager _uiWindows = default!;

    [UISystemDependency] private readonly ActionsSystem _actionsSystem = default!;
    [UISystemDependency] private readonly InteractionOutlineSystem _interactionOutlineSystem = default!;
    [UISystemDependency] private readonly TargetOutlineSystem _targetOutlineSystem = default!;
    private ActionButtonContainer? actionContainer;
    private ActionPage? _defaultPage;
    private List<ActionPage> _actionPages = new();

    private ActionsWindow? _window;
    private MenuButton ActionButton => _hud.GetUIWidget<MenuBar>().ActionButton;

    public override void OnStateChanged(StateChangedEventArgs args)
    {
        if (args.NewState is GameplayState)
        {
            ActionButton.OnPressed += ActionButtonPressed;

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.OpenActionsMenu,
                    InputCmdHandler.FromDelegate(_ => ToggleWindow()))
                .Register<ActionUIController>();
        }
    }

    private void ActionButtonPressed(ButtonEventArgs args)
    {
        ToggleWindow();
    }

    private void CreateWindow()
    {
        _window = _uiWindows.CreateNamedWindow<ActionsWindow>("Actions");

        if (_window == null)
            return;

        _window.OpenCentered();
        ActionButton.Pressed = true;
    }

    private void CloseWindow()
    {
        if (_window == null)
            return;

        _window.Dispose();
        _window = null;
        ActionButton.Pressed = false;
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

    public void RegisterActionContainer(ActionButtonContainer actionBar)
    {
        if (actionContainer != null)
        {
            Logger.Warning("Action container already defined for UI controller");
            return;
        }
        actionContainer = actionBar;
    }
    public void ClearActionContainer()
    {
        actionContainer = null;
    }
    public void ClearActionBars()
    {
        actionContainer = null;
    }
    public override void OnSystemLoaded(IEntitySystem system)
    {
        if (system is ActionsSystem)
        {
            ActionSystemStart();
        }
    }

    public override void OnSystemUnloaded(IEntitySystem system)
    {
        if (system is ActionsSystem)
        {
            ActionSystemShutdown();
        }
    }

    private void ActionSystemStart()
    {
        _actionsSystem.OnLinkActions += OnComponentLinked;
        _actionsSystem.OnUnlinkActions += OnComponentUnlinked;
    }

    private void ActionSystemShutdown()
    {
        _actionsSystem.OnLinkActions = null;
        _actionsSystem.OnUnlinkActions = null;
    }

    private void OnComponentUnlinked()
    {
        _defaultPage = null;
        actionContainer?.ClearActionData();
        //TODO: Clear button data
    }

    private void OnComponentLinked(ActionsComponent component)
    {
        LoadDefaultActions(component);
        if (_defaultPage != null) actionContainer?.LoadActionData(_defaultPage);
    }



    private void LoadDefaultActions(ActionsComponent component)
    {
        List<ActionType> actionsToadd = new();
        foreach (var actionType in component.Actions)
        {
            if (actionType.AutoPopulate)
            {
                actionsToadd.Add(actionType);
            }
        }
        _defaultPage = new ActionPage(actionsToadd.ToArray());
    }



    //TODO: Serialize this shit
    private sealed class ActionPage
    {
        public readonly List<ActionType?> Data;

        public ActionPage(SortedSet<ActionType> actions)
        {
            Data = new List<ActionType?>(actions);
        }

        public ActionPage(params ActionType?[] actions)
        {
            Data = new List<ActionType?>(actions);
        }
        public ActionType? this[int index]
        {
            get => Data[index];
            set => Data[index] = value;
        }

        public static implicit operator ActionType?[](ActionPage p)
        {
            return p.Data.ToArray();
        }
    }

}
