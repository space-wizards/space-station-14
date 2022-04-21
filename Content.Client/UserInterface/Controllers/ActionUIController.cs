using System.Linq;
using Content.Client.Actions;
using Content.Client.DragDrop;
using Content.Client.Gameplay;
using Content.Client.HUD;
using Content.Client.Outline;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.UIWindows;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using static Content.Client.UserInterface.UIWindows.ActionsWindow;
using static Robust.Client.UserInterface.Controls.BaseButton;
using static Robust.Client.UserInterface.Controls.LineEdit;
using static Robust.Client.UserInterface.Controls.MultiselectOptionButton<Content.Client.UserInterface.UIWindows.ActionsWindow.Filters>;
using MenuBar = Content.Client.HUD.Widgets.MenuBar;

namespace Content.Client.UserInterface.Controllers;

public sealed class ActionUIController : UIController, IOnStateChanged<GameplayState>, IPostInjectInit
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IHudManager _hud = default!;
    [Dependency] private readonly IUIWindowManager _uiWindows = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    [UISystemDependency] private readonly ActionsSystem _actionsSystem = default!;
    [UISystemDependency] private readonly InteractionOutlineSystem _interactionOutlineSystem = default!;
    [UISystemDependency] private readonly TargetOutlineSystem _targetOutlineSystem = default!;

    private ActionButtonContainer? _container;
    private ActionPage? _defaultPage;
    private List<ActionPage> _actionPages = new();
    private DragDropHelper<ActionButton> _dragDropHelper;
    private TextureRect _dragShadow;

    private ActionsWindow? _window;
    private MenuButton ActionButton => _hud.GetUIWidget<MenuBar>().ActionButton;

    public ActionUIController()
    {
        _dragDropHelper = new DragDropHelper<ActionButton>(OnBeginDrag, OnContinueDrag, OnEndDrag);
        _dragShadow = new TextureRect
        {
            MinSize = (64, 64),
            Stretch = TextureRect.StretchMode.Scale,
            Visible = false,
            SetSize = (64, 64)
        };
    }

    public void OnStateChanged(GameplayState state)
    {
        ActionButton.OnPressed += ActionButtonPressed;

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenActionsMenu,
                InputCmdHandler.FromDelegate(_ => ToggleWindow()))
            .Register<ActionUIController>();
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

        _window.OnClose += OnWindowClosed;
        _window.ClearButton.OnPressed += OnClearPressed;
        _window.SearchBar.OnTextChanged += OnSearchChanged;
        _window.FilterButton.OnItemSelected += OnFilterSelected;

        _window.OpenCentered();

        UpdateFilterLabel();
        SearchAndDisplay();

        ActionButton.Pressed = true;
    }

    private void CloseWindow()
    {
        _window?.Close();
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

    private void UpdateFilterLabel()
    {
        if (_window == null)
            return;

        if (_window.FilterButton.SelectedKeys.Count == 0)
        {
            _window.FilterLabel.Visible = false;
        }
        else
        {
            _window.FilterLabel.Visible = true;
            _window.FilterLabel.Text = Loc.GetString("ui-actionmenu-filter-label",
                ("selectedLabels", string.Join(", ", _window.FilterButton.SelectedLabels)));
        }
    }

    private bool MatchesFilter(ActionType action, Filters filter)
    {
        return filter switch
        {
            Filters.Enabled => action.Enabled,
            Filters.Item => action.Provider != null && action.Provider != _actionsSystem.PlayerActions?.Owner,
            Filters.Innate => action.Provider == null || action.Provider == _actionsSystem.PlayerActions?.Owner,
            Filters.Instant => action is InstantAction,
            Filters.Targeted => action is TargetedAction,
            _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, null)
        };
    }

    private void ClearList()
    {
        _window?.ResultsGrid.RemoveAllChildren();
    }

    private void PopulateActions(IEnumerable<ActionType> actions)
    {
        ClearList();

        foreach (var action in actions)
        {
            var actionItem = new ActionButton();
            actionItem.UpdateButtonData(_entities, action);
            actionItem.OnKeyBindDown += args => ActionKeyBindDown(args, actionItem);
            actionItem.OnKeyBindUp += ActionKeyBindUp;
            actionItem.ActionPressed += OnActionPressed;
        }
    }

    private void SearchAndDisplay()
    {
        if (_window == null)
            return;

        var search = _window.SearchBar.Text;
        var filters = _window.FilterButton.SelectedKeys;
        if (filters.Count == 0 && string.IsNullOrWhiteSpace(search))
        {
            ClearList();
            return;
        }

        IEnumerable<ActionType>? actions = _actionsSystem.PlayerActions?.Actions;
        if (actions == null)
        {
            actions = Array.Empty<ActionType>();
        }

        actions = actions.Where(action =>
        {
            if (filters.Count > 0 && filters.Any(filter => !MatchesFilter(action, filter)))
                return false;

            if (action.Keywords.Any(keyword => search.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                return true;

            if (action.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase))
                return true;

            if (action.Provider == null || action.Provider == _actionsSystem.PlayerActions?.Owner)
                return false;

            var name = _entities.GetComponent<MetaDataComponent>(action.Provider.Value).EntityName;
            return name.Contains(search, StringComparison.OrdinalIgnoreCase);
        });

        PopulateActions(actions);
    }

    private void OnWindowClosed()
    {
        _window = null;
        ActionButton.Pressed = false;
    }

    private void OnClearPressed(ButtonEventArgs args)
    {
        if (_window == null)
            return;

        _window.SearchBar.Clear();
        _window.FilterButton.DeselectAll();
        UpdateFilterLabel();
        SearchAndDisplay();
    }

    private void OnSearchChanged(LineEditEventArgs args)
    {
        SearchAndDisplay();
    }

    private void OnFilterSelected(ItemPressedEventArgs args)
    {
        UpdateFilterLabel();
        SearchAndDisplay();
    }

    private void ActionKeyBindUp(GUIBoundKeyEventArgs args)
    {
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        if (_ui.CurrentlyHovered is ActionButton button)
        {
            if (!_dragDropHelper.IsDragging || _dragDropHelper.Dragged?.Action == null)
            {
                _dragDropHelper.EndDrag();
                return;
            }

            button.UpdateButtonData(_entities, _dragDropHelper.Dragged.Action);
        }

        _dragDropHelper.EndDrag();
    }

    private void ActionKeyBindDown(GUIBoundKeyEventArgs args, ActionButton action)
    {
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        _dragDropHelper.MouseDown(action);
    }

    private void OnActionPressed(GUIBoundKeyEventArgs args, ActionButton button)
    {
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        _actionsSystem.TriggerAction(button.Action);
    }

    private bool OnBeginDrag()
    {
        _dragShadow.Texture = _dragDropHelper.Dragged?.IconTexture;
        LayoutContainer.SetPosition(_dragShadow, _ui.MousePositionScaled.Position - (32, 32));
        return true;
    }

    private bool OnContinueDrag(float frameTime)
    {
        LayoutContainer.SetPosition(_dragShadow, _ui.MousePositionScaled.Position - (32, 32));
        _dragShadow.Visible = true;
        return true;
    }

    private void OnEndDrag()
    {
        _dragShadow.Visible = false;
    }

    public void RegisterActionContainer(ActionButtonContainer container)
    {
        if (_container != null)
        {
            Logger.Warning("Action container already defined for UI controller");
            return;
        }

        _container = container;
        _container.ActionPressed += OnActionPressed;
    }

    public void ClearActionContainer()
    {
        _container = null;
    }

    public void ClearActionBars()
    {
        _container = null;
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
        _container?.ClearActionData();
        //TODO: Clear button data
    }

    private void OnComponentLinked(ActionsComponent component)
    {
        LoadDefaultActions(component);
        if (_defaultPage != null) _container?.LoadActionData(_defaultPage);
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

    void IPostInjectInit.PostInject()
    {
        _ui.PopupRoot.AddChild(_dragShadow);
    }
}
