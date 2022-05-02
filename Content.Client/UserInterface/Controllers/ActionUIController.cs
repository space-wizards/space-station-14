using System.Linq;
using Content.Client.Actions;
using Content.Client.DragDrop;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.UIWindows;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Timing;
using static Content.Client.UserInterface.UIWindows.ActionsWindow;
using static Robust.Client.UserInterface.Control;
using static Robust.Client.UserInterface.Controls.BaseButton;
using static Robust.Client.UserInterface.Controls.LineEdit;
using static Robust.Client.UserInterface.Controls.MultiselectOptionButton<Content.Client.UserInterface.UIWindows.ActionsWindow.Filters>;
using static Robust.Client.UserInterface.Controls.TextureRect;
using MenuBar = Content.Client.UserInterface.Widgets.MenuBar;

namespace Content.Client.UserInterface.Controllers;

public sealed class ActionUIController : UIController, IOnStateChanged<GameplayState>
{
    [Dependency] private readonly IEntityManager _entities = default!;

    [UISystemDependency] private readonly ActionsSystem _actionsSystem = default!;

    private ActionButtonContainer? _container;
    private ActionPage? _defaultPage;
    private readonly DragDropHelper<ActionButton> _menuDragHelper;
    private readonly TextureRect _dragShadow;

    private ActionsWindow? _window;
    private MenuButton ActionButton => UIManager.GetActiveUIWidget<MenuBar>().ActionButton;
    public ActionUIController()
    {
        _menuDragHelper = new DragDropHelper<ActionButton>(OnMenuBeginDrag, OnMenuContinueDrag, OnMenuEndDrag);
        _dragShadow = new TextureRect
        {
            MinSize = (64, 64),
            Stretch = StretchMode.Scale,
            Visible = false,
            SetSize = (64, 64),
            MouseFilter = MouseFilterMode.Ignore
        };
    }

    public void OnStateChanged(GameplayState state)
    {
        UIManager.PopupRoot.AddChild(_dragShadow);
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
        _window = UIManager.CreateNamedWindow<ActionsWindow>("Actions");

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
        if (_window == null)
            return;

        ClearList();

        foreach (var action in actions)
        {
            var actionItem = new ActionButton();
            actionItem.UpdateData(_entities, action);
            actionItem.ActionPressed += OnWindowActionPressed;
            actionItem.ActionUnpressed += OnWindowActionUnPressed;
            actionItem.ActionFocusExited += OnWindowActionFocusExisted;

            _window.ResultsGrid.AddChild(actionItem);
        }
    }

    private void SearchAndDisplay()
    {
        if (_window == null)
            return;

        var search = _window.SearchBar.Text;
        var filters = _window.FilterButton.SelectedKeys;

        IEnumerable<ActionType>? actions = _actionsSystem.PlayerActions?.Actions;
        actions ??= Array.Empty<ActionType>();

        if (filters.Count == 0 && string.IsNullOrWhiteSpace(search))
        {
            PopulateActions(actions);
            return;
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

    private void OnWindowActionPressed(GUIBoundKeyEventArgs args, ActionButton action)
    {
        if (args.Function != EngineKeyFunctions.UIClick && args.Function != EngineKeyFunctions.Use)
            return;

        _menuDragHelper.MouseDown(action);
        args.Handle();
    }

    private void OnWindowActionUnPressed(GUIBoundKeyEventArgs args, ActionButton dragged)
    {
        if (args.Function != EngineKeyFunctions.UIClick && args.Function != EngineKeyFunctions.Use)
            return;

        args.Handle();

        if (UIManager.CurrentlyHovered is ActionButton button)
        {
            if (!_menuDragHelper.IsDragging || _menuDragHelper.Dragged?.Action == null)
            {
                _menuDragHelper.EndDrag();
                return;
            }

            button.UpdateData(_entities, _menuDragHelper.Dragged.Action);
        }

        _menuDragHelper.EndDrag();
    }

    private void OnWindowActionFocusExisted(ActionButton button)
    {
        _menuDragHelper.EndDrag();
    }

    private void OnActionPressed(GUIBoundKeyEventArgs args, ActionButton button)
    {
        if (args.Function == EngineKeyFunctions.UIClick)
        {
            _actionsSystem.TriggerAction(button.Action);
            args.Handle();
        }
        else if (args.Function == EngineKeyFunctions.UIRightClick)
        {
            button.ClearData();
            args.Handle();
        }
    }

    private bool OnMenuBeginDrag()
    {
        _dragShadow.Texture = _menuDragHelper.Dragged?.IconTexture;
        LayoutContainer.SetPosition(_dragShadow, UIManager.MousePositionScaled.Position - (32, 32));
        return true;
    }

    private bool OnMenuContinueDrag(float frameTime)
    {
        LayoutContainer.SetPosition(_dragShadow, UIManager.MousePositionScaled.Position - (32, 32));
        _dragShadow.Visible = true;
        return true;
    }

    private void OnMenuEndDrag()
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

    public override void FrameUpdate(FrameEventArgs args)
    {
        _menuDragHelper.Update(args.DeltaSeconds);
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
}
