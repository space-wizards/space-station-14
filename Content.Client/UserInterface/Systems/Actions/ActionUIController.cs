using System.Linq;
using System.Runtime.InteropServices;
using Content.Client.Actions;
using Content.Client.DragDrop;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Actions.Controls;
using Content.Client.UserInterface.Systems.Actions.Widgets;
using Content.Client.UserInterface.Systems.Actions.Windows;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Timing;
using static Content.Client.Actions.ActionsSystem;
using static Content.Client.UserInterface.Systems.Actions.Windows.ActionsWindow;
using static Robust.Client.UserInterface.Control;
using static Robust.Client.UserInterface.Controls.BaseButton;
using static Robust.Client.UserInterface.Controls.LineEdit;
using static Robust.Client.UserInterface.Controls.MultiselectOptionButton<Content.Client.UserInterface.Systems.Actions.Windows.ActionsWindow.Filters>;
using static Robust.Client.UserInterface.Controls.TextureRect;

namespace Content.Client.UserInterface.Systems.Actions;

public sealed class ActionUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>,IOnSystemChanged<ActionsSystem>
{
    [Dependency] private readonly IEntityManager _entities = default!;

    [UISystemDependency] private readonly ActionsSystem _actionsSystem = default!;

    private const int PageAmount = 9;
    private const int ButtonAmount = 10;

    private ActionButtonContainer? _container;
    private readonly List<ActionPage> _pages = new();
    private readonly ActionPage _defaultPage;
    private int _currentPageIndex;
    private readonly DragDropHelper<ActionButton> _menuDragHelper;
    private readonly TextureRect _dragShadow;
    private ActionsWindow? _window;

    private ActionsBar ActionsBar => UIManager.GetActiveUIWidget<ActionsBar>();
    private MenuButton ActionButton => UIManager.GetActiveUIWidget<MenuBar.Widgets.MenuBar>().ActionButton;
    private ActionPage CurrentPage => _pages[_currentPageIndex];

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

        for (var i = 0; i < PageAmount; i++)
        {
            CreatePage();
        }

        _defaultPage = _pages[0];
    }

    public void OnStateEntered(GameplayState state)
    {
        ActionsBar.PageButtons.LeftArrow.OnPressed += OnLeftArrowPressed;
        ActionsBar.PageButtons.RightArrow.OnPressed += OnRightArrowPressed;
        ActionButton.OnPressed += ActionButtonPressed;
        _dragShadow.Orphan();
        UIManager.PopupRoot.AddChild(_dragShadow);
        CreateWindow();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenActionsMenu,
                InputCmdHandler.FromDelegate(_ => ToggleWindow()))
            .Bind(ContentKeyFunctions.Hotbar0,
                InputCmdHandler.FromDelegate(_ => TriggerAction(9)))
            .Bind(ContentKeyFunctions.Hotbar1,
                InputCmdHandler.FromDelegate(_ => TriggerAction(0)))
            .Bind(ContentKeyFunctions.Hotbar2,
                InputCmdHandler.FromDelegate(_ => TriggerAction(1)))
            .Bind(ContentKeyFunctions.Hotbar3,
                InputCmdHandler.FromDelegate(_ => TriggerAction(2)))
            .Bind(ContentKeyFunctions.Hotbar4,
                InputCmdHandler.FromDelegate(_ => TriggerAction(3)))
            .Bind(ContentKeyFunctions.Hotbar5,
                InputCmdHandler.FromDelegate(_ => TriggerAction(4)))
            .Bind(ContentKeyFunctions.Hotbar6,
                InputCmdHandler.FromDelegate(_ => TriggerAction(5)))
            .Bind(ContentKeyFunctions.Hotbar7,
                InputCmdHandler.FromDelegate(_ => TriggerAction(6)))
            .Bind(ContentKeyFunctions.Hotbar8,
                InputCmdHandler.FromDelegate(_ => TriggerAction(7)))
            .Bind(ContentKeyFunctions.Hotbar9,
                InputCmdHandler.FromDelegate(_ => TriggerAction(8)))
            .Bind(ContentKeyFunctions.Loadout1,
                InputCmdHandler.FromDelegate(_ => ChangePage(0)))
            .Bind(ContentKeyFunctions.Loadout2,
                InputCmdHandler.FromDelegate(_ => ChangePage(1)))
            .Bind(ContentKeyFunctions.Loadout3,
                InputCmdHandler.FromDelegate(_ => ChangePage(2)))
            .Bind(ContentKeyFunctions.Loadout4,
                InputCmdHandler.FromDelegate(_ => ChangePage(3)))
            .Bind(ContentKeyFunctions.Loadout5,
                InputCmdHandler.FromDelegate(_ => ChangePage(4)))
            .Bind(ContentKeyFunctions.Loadout6,
                InputCmdHandler.FromDelegate(_ => ChangePage(5)))
            .Bind(ContentKeyFunctions.Loadout7,
                InputCmdHandler.FromDelegate(_ => ChangePage(6)))
            .Bind(ContentKeyFunctions.Loadout8,
                InputCmdHandler.FromDelegate(_ => ChangePage(7)))
            .Bind(ContentKeyFunctions.Loadout9,
                InputCmdHandler.FromDelegate(_ => ChangePage(8)))
            .Register<ActionUIController>();
    }

    private void TriggerAction(int index)
    {
        if (CurrentPage[index] is not { } type)
            return;

        _actionsSystem.TriggerAction(type);
    }

    private void ChangePage(int index)
    {
        if (index < 0)
        {
            index = PageAmount - 1;
        }
        else if (index > PageAmount - 1)
        {
            index = 0;
        }

        _currentPageIndex = index;
        var page = _pages[_currentPageIndex];
        _container?.SetActionData(page);

        ActionsBar.PageButtons.Label.Text = $"{_currentPageIndex + 1}";
    }

    private void OnLeftArrowPressed(ButtonEventArgs args)
    {
        ChangePage(_currentPageIndex - 1);
    }

    private void OnRightArrowPressed(ButtonEventArgs args)
    {
        ChangePage(_currentPageIndex + 1);
    }

    private void ActionButtonPressed(ButtonEventArgs args)
    {
        ToggleWindow();
    }

    private void CreateWindow()
    {
        _window = UIManager.CreateWindow<ActionsWindow>();
        LayoutContainer.SetAnchorPreset(_window,LayoutContainer.LayoutPreset.CenterTop);
        _window.OnClose += OnWindowClosed;
        _window.OnOpen += OnWindowOpen;
        _window.ClearButton.OnPressed += OnClearPressed;
        _window.SearchBar.OnTextChanged += OnSearchChanged;
        _window.FilterButton.OnItemSelected += OnFilterSelected;
        UpdateFilterLabel();
        SearchAndDisplay();
    }

    private void ToggleWindow()
    {
        if (_window == null)
        {
            CreateWindow();
            return;
        }

        if (_window.IsOpen)
        {
            _window.Close();
            return;
        }
        _window.Open();

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
            var button = new ActionButton {Locked = true};

            button.UpdateData(_entities, action);
            button.ActionPressed += OnWindowActionPressed;
            button.ActionUnpressed += OnWindowActionUnPressed;
            button.ActionFocusExited += OnWindowActionFocusExisted;

            _window.ResultsGrid.AddChild(button);
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

            if (action.DisplayName.Contains((string) search, StringComparison.OrdinalIgnoreCase))
                return true;

            if (action.Provider == null || action.Provider == _actionsSystem.PlayerActions?.Owner)
                return false;

            var name = _entities.GetComponent<MetaDataComponent>(action.Provider.Value).EntityName;
            return name.Contains((string) search, StringComparison.OrdinalIgnoreCase);
        });

        PopulateActions(actions);
    }

    private void SetAction(ActionButton button, ActionType? type)
    {
        int position;

        if (type == null)
        {
            button.ClearData();
            if (_container?.TryGetButtonIndex(button, out position) ?? false)
            {
                CurrentPage[position] = type;
            }
            return;
        }

        if (button.TryReplaceWith(_entities, type) &&
            _container != null &&
            _container.TryGetButtonIndex(button, out position))
        {
            CurrentPage[position] = type;
        }
    }

    private void DragAction()
    {
        if (UIManager.CurrentlyHovered is ActionButton button)
        {
            if (!_menuDragHelper.IsDragging || _menuDragHelper.Dragged?.Action is not { } type)
            {
                _menuDragHelper.EndDrag();
                return;
            }

            SetAction(button, type);
        }

        if (_menuDragHelper.Dragged is { Parent: ActionButtonContainer } old)
        {
            SetAction(old, null);
        }

        _menuDragHelper.EndDrag();
    }

    private void OnWindowOpen()
    {
        ActionButton.Pressed = true;
    }

    private void OnWindowClosed()
    {
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

        DragAction();
        args.Handle();
    }

    private void OnWindowActionFocusExisted(ActionButton button)
    {
        _menuDragHelper.EndDrag();
    }

    private void OnActionPressed(GUIBoundKeyEventArgs args, ActionButton button)
    {
        if (args.Function == EngineKeyFunctions.UIClick)
        {
            _menuDragHelper.MouseDown(button);
            args.Handle();
        }
        else if (args.Function == EngineKeyFunctions.UIRightClick)
        {
            SetAction(button, null);
            args.Handle();
        }
    }

    private void OnActionUnpressed(GUIBoundKeyEventArgs args, ActionButton button)
    {
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        if (UIManager.CurrentlyHovered == button)
        {
            _actionsSystem.TriggerAction(button.Action);
            _menuDragHelper.EndDrag();
        }
        else
        {
            DragAction();
        }

        args.Handle();
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
        _container.ActionUnpressed += OnActionUnpressed;
    }

    public void ClearActions()
    {
        _container?.ClearActionData();
    }

    private void AssignSlots(List<SlotAssignment> assignments)
    {
        foreach (ref var assignment in CollectionsMarshal.AsSpan(assignments))
        {
            _pages[assignment.Hotbar][assignment.Slot] = assignment.Action;
        }

        _container?.SetActionData(_pages[_currentPageIndex]);
    }

    public void RemoveActionContainer()
    {
        _container = null;
    }

    public void OnSystemLoaded(ActionsSystem system)
    {
        _actionsSystem.OnLinkActions += OnComponentLinked;
        _actionsSystem.OnUnlinkActions += OnComponentUnlinked;
        _actionsSystem.ClearAssignments += ClearActions;
        _actionsSystem.AssignSlot += AssignSlots;
    }

    public void OnSystemUnloaded(ActionsSystem system)
    {
        _actionsSystem.OnLinkActions -= OnComponentLinked;
        _actionsSystem.OnUnlinkActions -= OnComponentUnlinked;
        _actionsSystem.ClearAssignments -= ClearActions;
        _actionsSystem.AssignSlot -= AssignSlots;
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        _menuDragHelper.Update(args.DeltaSeconds);
    }

    private void OnComponentLinked(ActionsComponent component)
    {
        LoadDefaultActions(component);
        _container?.SetActionData(_defaultPage);
    }

    private void OnComponentUnlinked()
    {
        _container?.ClearActionData();
        //TODO: Clear button data
    }

    private void CreatePage()
    {
        var page = new ActionPage(ButtonAmount);
        _pages.Add(page);
    }

    private void LoadDefaultActions(ActionsComponent component)
    {
        List<ActionType> actions = new();
        foreach (var actionType in component.Actions)
        {
            if (actionType.AutoPopulate)
            {
                actions.Add(actionType);
            }
        }

        if (actions.Count == 0)
        {
            return;
        }

        var loopedPageIndex = 0;
        var loopedPage = _pages[loopedPageIndex];
        loopedPage.Clear();

        for (var i = 0; i < actions.Count; i++)
        {
            var mod = i % ButtonAmount;
            if (i != 0 && mod == 0)
            {
                loopedPageIndex++;
                loopedPage = _pages[loopedPageIndex];
                loopedPage.Clear();
            }

            if (loopedPageIndex >= PageAmount - 1)
            {
                break;
            }

            loopedPage[mod] = actions[i];
        }
    }

    //TODO: Serialize this shit
    private sealed class ActionPage
    {
        private readonly ActionType?[] _data;

        public ActionPage(int size)
        {
            _data = new ActionType?[size];
        }

        public ActionType? this[int index]
        {
            get => _data[index];
            set => _data[index] = value;
        }

        public static implicit operator ActionType?[](ActionPage p)
        {
            return p._data.ToArray();
        }

        public void Clear()
        {
            Array.Fill(_data, null);
        }
    }

    public void OnStateExited(GameplayState state)
    {
        CommandBinds.Unregister<ActionUIController>();
    }
}
