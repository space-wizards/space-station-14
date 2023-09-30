using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Content.Client.Actions;
using Content.Client.Construction;
using Content.Client.DragDrop;
using Content.Client.Gameplay;
using Content.Client.Hands;
using Content.Client.Outline;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Actions.Controls;
using Content.Client.UserInterface.Systems.Actions.Widgets;
using Content.Client.UserInterface.Systems.Actions.Windows;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Shared.Actions;
using Content.Shared.Input;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Content.Client.Actions.ActionsSystem;
using static Content.Client.UserInterface.Systems.Actions.Windows.ActionsWindow;
using static Robust.Client.UserInterface.Control;
using static Robust.Client.UserInterface.Controls.BaseButton;
using static Robust.Client.UserInterface.Controls.LineEdit;
using static Robust.Client.UserInterface.Controls.MultiselectOptionButton<
    Content.Client.UserInterface.Systems.Actions.Windows.ActionsWindow.Filters>;
using static Robust.Client.UserInterface.Controls.TextureRect;
using static Robust.Shared.Input.Binding.PointerInputCmdHandler;

namespace Content.Client.UserInterface.Systems.Actions;

public sealed class ActionUIController : UIController, IOnStateChanged<GameplayState>, IOnSystemChanged<ActionsSystem>
{
    [Dependency] private readonly IOverlayManager _overlays = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    [UISystemDependency] private readonly ActionsSystem? _actionsSystem = default;
    [UISystemDependency] private readonly InteractionOutlineSystem? _interactionOutline = default;
    [UISystemDependency] private readonly TargetOutlineSystem? _targetOutline = default;
    [UISystemDependency] private readonly SpriteSystem _spriteSystem = default!;

    private const int DefaultPageIndex = 0;
    private ActionButtonContainer? _container;
    private readonly List<ActionPage> _pages = new();
    private int _currentPageIndex = DefaultPageIndex;
    private readonly DragDropHelper<ActionButton> _menuDragHelper;
    private readonly TextureRect _dragShadow;
    private ActionsWindow? _window;

    private ActionsBar? ActionsBar => UIManager.GetActiveUIWidgetOrNull<ActionsBar>();
    private MenuButton? ActionButton => UIManager.GetActiveUIWidgetOrNull<MenuBar.Widgets.GameTopMenuBar>()?.ActionButton;
    private ActionPage CurrentPage => _pages[_currentPageIndex];

    public bool IsDragging => _menuDragHelper.IsDragging;

    /// <summary>
    /// Action slot we are currently selecting a target for.
    /// </summary>
    public EntityUid? SelectingTargetFor { get; private set; }

    public ActionUIController()
    {
        _menuDragHelper = new DragDropHelper<ActionButton>(OnMenuBeginDrag, OnMenuContinueDrag, OnMenuEndDrag);
        _dragShadow = new TextureRect
        {
            MinSize = new Vector2(64, 64),
            Stretch = StretchMode.Scale,
            Visible = false,
            SetSize = new Vector2(64, 64),
            MouseFilter = MouseFilterMode.Ignore
        };

        var pageCount = ContentKeyFunctions.GetLoadoutBoundKeys().Length;
        var buttonCount = ContentKeyFunctions.GetHotbarBoundKeys().Length;
        for (var i = 0; i < pageCount; i++)
        {
            var page = new ActionPage(buttonCount);
            _pages.Add(page);
        }
    }

    public override void Initialize()
    {
        base.Initialize();

        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += OnScreenLoad;
        gameplayStateLoad.OnScreenUnload += OnScreenUnload;
    }

    private void OnScreenLoad()
    {
       LoadGui();
    }

    private void OnScreenUnload()
    {
        UnloadGui();
    }

    public void OnStateEntered(GameplayState state)
    {
        if (_actionsSystem != null)
        {
            _actionsSystem.OnActionAdded += OnActionAdded;
            _actionsSystem.OnActionRemoved += OnActionRemoved;
            _actionsSystem.ActionReplaced += OnActionReplaced;
            _actionsSystem.ActionsUpdated += OnActionsUpdated;
        }

        UpdateFilterLabel();
        SearchAndDisplay();

        _dragShadow.Orphan();
        UIManager.PopupRoot.AddChild(_dragShadow);

        var builder = CommandBinds.Builder;
        var hotbarKeys = ContentKeyFunctions.GetHotbarBoundKeys();
        for (var i = 0; i < hotbarKeys.Length; i++)
        {
            var boundId = i; // This is needed, because the lambda captures it.
            var boundKey = hotbarKeys[i];
            builder = builder.Bind(boundKey, new PointerInputCmdHandler((in PointerInputCmdArgs args) =>
            {
                if (args.State != BoundKeyState.Up)
                    return false;

                TriggerAction(boundId);
                return true;
            }, false, true));
        }

        var loadoutKeys = ContentKeyFunctions.GetLoadoutBoundKeys();
        for (var i = 0; i < loadoutKeys.Length; i++)
        {
            var boundId = i; // This is needed, because the lambda captures it.
            var boundKey = loadoutKeys[i];
            builder = builder.Bind(boundKey,
                InputCmdHandler.FromDelegate(_ => ChangePage(boundId)));
        }

        builder
            .Bind(ContentKeyFunctions.OpenActionsMenu,
                InputCmdHandler.FromDelegate(_ => ToggleWindow()))
            .BindBefore(EngineKeyFunctions.Use, new PointerInputCmdHandler(TargetingOnUse, outsidePrediction: true),
                    typeof(ConstructionSystem), typeof(DragDropSystem))
                .BindBefore(EngineKeyFunctions.UIRightClick, new PointerInputCmdHandler(TargetingCancel, outsidePrediction: true))
            .Register<ActionUIController>();
    }

    private bool TargetingCancel(in PointerInputCmdArgs args)
    {
        if (!_timing.IsFirstTimePredicted)
            return false;

        // only do something for actual target-based actions
        if (SelectingTargetFor == null)
            return false;

        StopTargeting();
        return true;
    }

    /// <summary>
    ///     If the user clicked somewhere, and they are currently targeting an action, try and perform it.
    /// </summary>
    private bool TargetingOnUse(in PointerInputCmdArgs args)
    {
        if (!_timing.IsFirstTimePredicted || _actionsSystem == null || SelectingTargetFor is not { } actionId)
            return false;

        if (_playerManager.LocalPlayer?.ControlledEntity is not { } user)
            return false;

        if (!EntityManager.TryGetComponent(user, out ActionsComponent? comp))
            return false;

        if (!_actionsSystem.TryGetActionData(actionId, out var baseAction) ||
            baseAction is not BaseTargetActionComponent action)
        {
            return false;
        }

        // Is the action currently valid?
        if (!action.Enabled
            || action.Charges is 0
            || action.Cooldown.HasValue && action.Cooldown.Value.End > _timing.CurTime)
        {
            // The user is targeting with this action, but it is not valid. Maybe mark this click as
            // handled and prevent further interactions.
            return !action.InteractOnMiss;
        }

        switch (action)
        {
            case WorldTargetActionComponent mapTarget:
                    return TryTargetWorld(args, actionId, mapTarget, user, comp) || !mapTarget.InteractOnMiss;

            case EntityTargetActionComponent entTarget:
                    return TryTargetEntity(args, actionId, entTarget, user, comp) || !entTarget.InteractOnMiss;

            default:
                Logger.Error($"Unknown targeting action: {actionId.GetType()}");
                return false;
        }
    }

    private bool TryTargetWorld(in PointerInputCmdArgs args, EntityUid actionId, WorldTargetActionComponent action, EntityUid user, ActionsComponent actionComp)
    {
        if (_actionsSystem == null)
            return false;

        var coords = args.Coordinates;

        if (!_actionsSystem.ValidateWorldTarget(user, coords, action))
        {
            // Invalid target.
            if (action.DeselectOnMiss)
                StopTargeting();

            return false;
        }

        if (action.ClientExclusive)
        {
            if (action.Event != null)
            {
                action.Event.Target = coords;
                action.Event.Performer = user;
            }

            _actionsSystem.PerformAction(user, actionComp, actionId, action, action.Event, _timing.CurTime);
        }
        else
            EntityManager.RaisePredictiveEvent(new RequestPerformActionEvent(EntityManager.GetNetEntity(actionId), EntityManager.GetNetCoordinates(coords)));

        if (!action.Repeat)
            StopTargeting();

        return true;
    }

    private bool TryTargetEntity(in PointerInputCmdArgs args, EntityUid actionId, EntityTargetActionComponent action, EntityUid user, ActionsComponent actionComp)
    {
        if (_actionsSystem == null)
            return false;

        var entity = args.EntityUid;

        if (!_actionsSystem.ValidateEntityTarget(user, entity, action))
        {
            if (action.DeselectOnMiss)
                StopTargeting();

            return false;
        }

        if (action.ClientExclusive)
        {
            if (action.Event != null)
            {
                action.Event.Target = entity;
                action.Event.Performer = user;
            }

            _actionsSystem.PerformAction(user, actionComp, actionId, action, action.Event, _timing.CurTime);
        }
        else
            EntityManager.RaisePredictiveEvent(new RequestPerformActionEvent(EntityManager.GetNetEntity(actionId), EntityManager.GetNetEntity(args.EntityUid)));

        if (!action.Repeat)
            StopTargeting();

        return true;
    }

    public void UnloadButton()
    {
        if (ActionButton == null)
        {
            return;
        }

        ActionButton.OnPressed -= ActionButtonPressed;
    }

    public void LoadButton()
    {
        if (ActionButton == null)
        {
            return;
        }

        ActionButton.OnPressed += ActionButtonPressed;
    }

    private void OnWindowOpened()
    {
        if (ActionButton != null)
            ActionButton.Pressed = true;
    }

    private void OnWindowClosed()
    {
        if (ActionButton != null)
            ActionButton.Pressed = false;
    }

    public void OnStateExited(GameplayState state)
    {
        if (_actionsSystem != null)
        {
            _actionsSystem.OnActionAdded -= OnActionAdded;
            _actionsSystem.OnActionRemoved -= OnActionRemoved;
            _actionsSystem.ActionReplaced -= OnActionReplaced;
            _actionsSystem.ActionsUpdated -= OnActionsUpdated;
        }

        CommandBinds.Unregister<ActionUIController>();
    }

    private void TriggerAction(int index)
    {
        if (_actionsSystem == null ||
            CurrentPage[index] is not { } actionId ||
            !_actionsSystem.TryGetActionData(actionId, out var baseAction))
        {
            return;
        }

        if (baseAction is BaseTargetActionComponent action)
            ToggleTargeting(actionId, action);
        else
            _actionsSystem?.TriggerAction(actionId, baseAction);
    }

    private void ChangePage(int index)
    {
        var lastPage = _pages.Count - 1;
        if (index < 0)
        {
            index = lastPage;
        }
        else if (index > lastPage)
        {
            index = 0;
        }

        _currentPageIndex = index;
        var page = _pages[_currentPageIndex];
        _container?.SetActionData(page);

        ActionsBar!.PageButtons.Label.Text = $"{_currentPageIndex + 1}";
    }

    private void OnLeftArrowPressed(ButtonEventArgs args)
    {
        ChangePage(_currentPageIndex - 1);
    }

    private void OnRightArrowPressed(ButtonEventArgs args)
    {
        ChangePage(_currentPageIndex + 1);
    }

    private void AppendAction(EntityUid action)
    {
        if (_container == null)
            return;

        foreach (var button in _container.GetButtons())
        {
            if (button.ActionId != null)
                continue;

            SetAction(button, action);
            return;
        }

        foreach (var page in _pages)
        {
            for (var i = 0; i < page.Size; i++)
            {
                var pageAction = page[i];
                if (pageAction != null)
                    continue;

                page[i] = action;
                return;
            }
        }
    }

    private void OnActionAdded(EntityUid actionId)
    {
        if (_actionsSystem == null ||
            !_actionsSystem.TryGetActionData(actionId, out var action))
        {
            return;
        }

        // if the action is toggled when we add it, start targeting
        if (action is BaseTargetActionComponent targetAction && action.Toggled)
            StartTargeting(actionId, targetAction);

        foreach (var page in _pages)
        {
            for (var i = 0; i < page.Size; i++)
            {
                if (page[i] == actionId)
                {
                    return;
                }
            }
        }

        AppendAction(actionId);
        SearchAndDisplay();
    }

    private void OnActionRemoved(EntityUid actionId)
    {
        if (_container == null)
            return;

        // stop targeting if the action is removed
        if (actionId == SelectingTargetFor)
            StopTargeting();

        foreach (var button in _container.GetButtons())
        {
            if (button.ActionId == actionId)
            {
                SetAction(button, null);
            }
        }

        foreach (var page in _pages)
        {
            for (var i = 0; i < page.Size; i++)
            {
                if (page[i] == actionId)
                {
                    page[i] = null;
                }
            }
        }

        SearchAndDisplay();
    }

    private void OnActionReplaced(EntityUid actionId)
    {
        if (_container == null)
            return;

        foreach (var button in _container.GetButtons())
        {
            if (button.ActionId == actionId)
                button.UpdateData(actionId);
        }
    }

    private void OnActionsUpdated()
    {
        if (_container == null)
            return;

        foreach (var button in _container.GetButtons())
        {
            button.UpdateIcons();
        }
    }

    private void ActionButtonPressed(ButtonEventArgs args)
    {
        ToggleWindow();
    }

    private void ToggleWindow()
    {
        if (_window == null)
            return;

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

    private bool MatchesFilter(BaseActionComponent action, Filters filter)
    {
        return filter switch
        {
            Filters.Enabled => action.Enabled,
            Filters.Item => action.Container != null && action.Container != _playerManager.LocalPlayer?.ControlledEntity,
            Filters.Innate => action.Container == null || action.Container == _playerManager.LocalPlayer?.ControlledEntity,
            Filters.Instant => action is InstantActionComponent,
            Filters.Targeted => action is BaseTargetActionComponent,
            _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, null)
        };
    }

    private void ClearList()
    {
        if (_window?.Disposed == false)
            _window.ResultsGrid.RemoveAllChildren();
    }

    private void PopulateActions(IEnumerable<(EntityUid Id, BaseActionComponent Comp)> actions)
    {
        if (_window == null)
            return;

        ClearList();

        foreach (var action in actions)
        {
            var button = new ActionButton {Locked = true};

            button.UpdateData(action.Id);
            button.ActionPressed += OnWindowActionPressed;
            button.ActionUnpressed += OnWindowActionUnPressed;
            button.ActionFocusExited += OnWindowActionFocusExisted;

            _window.ResultsGrid.AddChild(button);
        }
    }

    private void SearchAndDisplay()
    {
        if (_window is not { Disposed: false } || _actionsSystem == null)
            return;

        if (_playerManager.LocalPlayer?.ControlledEntity is not { } player)
            return;

        var search = _window.SearchBar.Text;
        var filters = _window.FilterButton.SelectedKeys;
        var actions = _actionsSystem.GetClientActions();

        if (filters.Count == 0 && string.IsNullOrWhiteSpace(search))
        {
            PopulateActions(actions);
            return;
        }

        actions = actions.Where(action =>
        {
            if (filters.Count > 0 && filters.Any(filter => !MatchesFilter(action.Comp, filter)))
                return false;

            if (action.Comp.Keywords.Any(keyword => search.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                return true;

            var name = EntityManager.GetComponent<MetaDataComponent>(action.Id).EntityName;
            if (name.Contains(search, StringComparison.OrdinalIgnoreCase))
                return true;

            if (action.Comp.Container == null || action.Comp.Container == player)
                return false;

            var providerName = EntityManager.GetComponent<MetaDataComponent>(action.Comp.Container.Value).EntityName;
            return providerName.Contains(search, StringComparison.OrdinalIgnoreCase);
        });

        PopulateActions(actions);
    }

    private void SetAction(ActionButton button, EntityUid? actionId)
    {
        int position;

        if (actionId == null)
        {
            button.ClearData();
            if (_container?.TryGetButtonIndex(button, out position) ?? false)
            {
                CurrentPage[position] = actionId;
            }

            return;
        }

        if (button.TryReplaceWith(actionId.Value) &&
            _container != null &&
            _container.TryGetButtonIndex(button, out position))
        {
            CurrentPage[position] = actionId;
        }
    }

    private void DragAction()
    {
        if (UIManager.CurrentlyHovered is ActionButton button)
        {
            if (!_menuDragHelper.IsDragging || _menuDragHelper.Dragged?.ActionId is not { } type)
            {
                _menuDragHelper.EndDrag();
                return;
            }

            SetAction(button, type);
        }

        if (_menuDragHelper.Dragged is {Parent: ActionButtonContainer} old)
        {
            SetAction(old, null);
        }

        _menuDragHelper.EndDrag();
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
            if (button.ActionId == null)
            {
                var ev = new FillActionSlotEvent();
                EntityManager.EventBus.RaiseEvent(EventSource.Local, ev);
                if (ev.Action != null)
                    SetAction(button, ev.Action);
            }
            else
            {
                _menuDragHelper.MouseDown(button);
            }

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
        if (args.Function != EngineKeyFunctions.UIClick || _actionsSystem == null)
            return;

        if (UIManager.CurrentlyHovered == button)
        {
            _menuDragHelper.EndDrag();

            if (_actionsSystem.TryGetActionData(button.ActionId, out var baseAction))
            {
                if (baseAction is BaseTargetActionComponent action)
                {
                    // for target actions, we go into "select target" mode, we don't
                    // message the server until we actually pick our target.

                    // if we're clicking the same thing we're already targeting for, then we simply cancel
                    // targeting
                    ToggleTargeting(button.ActionId.Value, action);
                    return;
                }

                _actionsSystem?.TriggerAction(button.ActionId.Value, baseAction);
            }
        }
        else
        {
            DragAction();
        }

        args.Handle();
    }

    private bool OnMenuBeginDrag()
    {
        if (_actionsSystem != null && _actionsSystem.TryGetActionData(_menuDragHelper.Dragged?.ActionId, out var action))
        {
            if (action.EntityIcon is {} entIcon)
            {
                _dragShadow.Texture = EntityManager.GetComponent<SpriteComponent>(entIcon).Icon?
                    .GetFrame(RsiDirection.South, 0);
            }
            else if (action.Icon != null)
            {
                _dragShadow.Texture = _spriteSystem.Frame0(action.Icon);
            }
            else
            {
                _dragShadow.Texture = null;
            }
        }

        LayoutContainer.SetPosition(_dragShadow, UIManager.MousePositionScaled.Position - new Vector2(32, 32));
        return true;
    }

    private bool OnMenuContinueDrag(float frameTime)
    {
        LayoutContainer.SetPosition(_dragShadow, UIManager.MousePositionScaled.Position - new Vector2(32, 32));
        _dragShadow.Visible = true;
        return true;
    }

    private void OnMenuEndDrag()
    {
        _dragShadow.Texture = null;
        _dragShadow.Visible = false;
    }

    private void UnloadGui()
    {
        _actionsSystem?.UnlinkAllActions();

        if (ActionsBar == null)
        {
            return;
        }

        ActionsBar.PageButtons.LeftArrow.OnPressed -= OnLeftArrowPressed;
        ActionsBar.PageButtons.RightArrow.OnPressed -= OnRightArrowPressed;

        if (_window != null)
        {
            _window.OnOpen -= OnWindowOpened;
            _window.OnClose -= OnWindowClosed;
            _window.ClearButton.OnPressed -= OnClearPressed;
            _window.SearchBar.OnTextChanged -= OnSearchChanged;
            _window.FilterButton.OnItemSelected -= OnFilterSelected;

            _window.Dispose();
            _window = null;
        }
    }

    private void LoadGui()
    {
        DebugTools.Assert(_window == null);
        _window = UIManager.CreateWindow<ActionsWindow>();
        LayoutContainer.SetAnchorPreset(_window, LayoutContainer.LayoutPreset.CenterTop);

        _window.OnOpen += OnWindowOpened;
        _window.OnClose += OnWindowClosed;
        _window.ClearButton.OnPressed += OnClearPressed;
        _window.SearchBar.OnTextChanged += OnSearchChanged;
        _window.FilterButton.OnItemSelected += OnFilterSelected;

        if (ActionsBar == null)
        {
            return;
        }

        ActionsBar.PageButtons.LeftArrow.OnPressed += OnLeftArrowPressed;
        ActionsBar.PageButtons.RightArrow.OnPressed += OnRightArrowPressed;

        RegisterActionContainer(ActionsBar.ActionsContainer);

        _actionsSystem?.LinkAllActions();
    }

    public void RegisterActionContainer(ActionButtonContainer container)
    {
        if (_container != null)
        {
            _container.ActionPressed -= OnActionPressed;
            _container.ActionUnpressed -= OnActionPressed;
        }

        _container = container;
        _container.ActionPressed += OnActionPressed;
        _container.ActionUnpressed += OnActionUnpressed;
    }

    private void ClearActions()
    {
        _container?.ClearActionData();
    }

    private void AssignSlots(List<SlotAssignment> assignments)
    {
        foreach (ref var assignment in CollectionsMarshal.AsSpan(assignments))
        {
            _pages[assignment.Hotbar][assignment.Slot] = assignment.ActionId;
        }

        _container?.SetActionData(_pages[_currentPageIndex]);
    }

    public void RemoveActionContainer()
    {
        _container = null;
    }

    public void OnSystemLoaded(ActionsSystem system)
    {
        system.LinkActions += OnComponentLinked;
        system.UnlinkActions += OnComponentUnlinked;
        system.ClearAssignments += ClearActions;
        system.AssignSlot += AssignSlots;
    }

    public void OnSystemUnloaded(ActionsSystem system)
    {
        system.LinkActions -= OnComponentLinked;
        system.UnlinkActions -= OnComponentUnlinked;
        system.ClearAssignments -= ClearActions;
        system.AssignSlot -= AssignSlots;
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        _menuDragHelper.Update(args.DeltaSeconds);
    }

    private void OnComponentLinked(ActionsComponent component)
    {
        LoadDefaultActions(component);
        _container?.SetActionData(_pages[DefaultPageIndex]);
        SearchAndDisplay();
    }

    private void OnComponentUnlinked()
    {
        _container?.ClearActionData();
        SearchAndDisplay();
        StopTargeting();
    }

    private void LoadDefaultActions(ActionsComponent component)
    {
        if (_actionsSystem == null)
            return;

        var actions = _actionsSystem.GetClientActions().Where(action => action.Comp.AutoPopulate).ToList();
        actions.Sort(ActionComparer);

        var offset = 0;
        var totalPages = _pages.Count;
        var pagesLeft = totalPages;
        var currentPage = DefaultPageIndex;
        while (pagesLeft > 0)
        {
            var page = _pages[currentPage];
            var pageSize = page.Size;

            for (var slot = 0; slot < pageSize; slot++)
            {
                var actionIndex = slot + offset;
                if (actionIndex < actions.Count)
                {
                    page[slot] = actions[slot + offset].Id;
                }
                else
                {
                    page[slot] = null;
                }
            }

            offset += pageSize;
            currentPage++;
            if (currentPage == totalPages)
            {
                currentPage = 0;
            }

            pagesLeft--;
        }
    }

    /// <summary>
    /// If currently targeting with this slot, stops targeting.
    /// If currently targeting with no slot or a different slot, switches to
    /// targeting with the specified slot.
    /// </summary>
    private void ToggleTargeting(EntityUid actionId, BaseTargetActionComponent action)
    {
        if (SelectingTargetFor == actionId)
        {
            StopTargeting();
            return;
        }

        StartTargeting(actionId, action);
    }

    /// <summary>
    /// Puts us in targeting mode, where we need to pick either a target point or entity
    /// </summary>
    private void StartTargeting(EntityUid actionId, BaseTargetActionComponent action)
    {
        // If we were targeting something else we should stop
        StopTargeting();

        SelectingTargetFor = actionId;
        // TODO inform the server
        action.Toggled = true;

        // override "held-item" overlay
        var provider = action.Container;

        if (action.TargetingIndicator && _overlays.TryGetOverlay<ShowHandItemOverlay>(out var handOverlay))
        {
            if (action.ItemIconStyle == ItemActionIconStyle.BigItem && action.Container != null)
            {
                handOverlay.EntityOverride = provider;
            }
            else if (action.Toggled && action.IconOn != null)
                handOverlay.IconOverride = _spriteSystem.Frame0(action.IconOn);
            else if (action.Icon != null)
                handOverlay.IconOverride = _spriteSystem.Frame0(action.Icon);
        }

        if (_container != null)
        {
            foreach (var button in _container.GetButtons())
            {
                if (button.ActionId == actionId)
                    button.UpdateIcons();
            }
        }

        // TODO: allow world-targets to check valid positions. E.g., maybe:
        // - Draw a red/green ghost entity
        // - Add a yes/no checkmark where the HandItemOverlay usually is

        // Highlight valid entity targets
        if (action is not EntityTargetActionComponent entityAction)
            return;

        Func<EntityUid, bool>? predicate = null;
        var attachedEnt = entityAction.AttachedEntity;

        if (!entityAction.CanTargetSelf)
            predicate = e => e != attachedEnt;

        var range = entityAction.CheckCanAccess ? action.Range : -1;

        _interactionOutline?.SetEnabled(false);
        _targetOutline?.Enable(range, entityAction.CheckCanAccess, predicate, entityAction.Whitelist, null);
    }

    /// <summary>
    /// Switch out of targeting mode if currently selecting target for an action
    /// </summary>
    private void StopTargeting()
    {
        if (SelectingTargetFor == null)
            return;

        var oldAction = SelectingTargetFor;
        if (_actionsSystem != null && _actionsSystem.TryGetActionData(oldAction, out var action))
        {
            // TODO inform the server
            action.Toggled = false;
        }

        SelectingTargetFor = null;

        _targetOutline?.Disable();
        _interactionOutline?.SetEnabled(true);

        if (_container != null)
        {
            foreach (var button in _container.GetButtons())
            {
                if (button.ActionId == oldAction)
                    button.UpdateIcons();
            }
        }

        if (!_overlays.TryGetOverlay<ShowHandItemOverlay>(out var handOverlay))
            return;

        handOverlay.IconOverride = null;
        handOverlay.EntityOverride = null;
    }


    //TODO: Serialize this shit
    private sealed class ActionPage
    {
        private readonly EntityUid?[] _data;

        public ActionPage(int size)
        {
            _data = new EntityUid?[size];
        }

        public EntityUid? this[int index]
        {
            get => _data[index];
            set => _data[index] = value;
        }

        public static implicit operator EntityUid?[](ActionPage p)
        {
            return p._data.ToArray();
        }

        public void Clear()
        {
            Array.Fill(_data, null);
        }

        public int Size => _data.Length;
    }
}
