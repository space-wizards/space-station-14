using System.Linq;
using System.Numerics;
using Content.Client.Actions;
using Content.Client.Construction;
using Content.Client.Gameplay;
using Content.Client.Hands;
using Content.Client.Interaction;
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
using Robust.Client.Input;
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
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IInputManager _input = default!;

    [UISystemDependency] private readonly ActionsSystem? _actionsSystem = default;
    [UISystemDependency] private readonly InteractionOutlineSystem? _interactionOutline = default;
    [UISystemDependency] private readonly TargetOutlineSystem? _targetOutline = default;
    [UISystemDependency] private readonly SpriteSystem _spriteSystem = default!;

    private ActionButtonContainer? _container;
    private readonly List<EntityUid?> _actions = new();
    private readonly DragDropHelper<ActionButton> _menuDragHelper;
    private readonly TextureRect _dragShadow;
    private ActionsWindow? _window;

    private ActionsBar? ActionsBar => UIManager.GetActiveUIWidgetOrNull<ActionsBar>();
    private MenuButton? ActionButton => UIManager.GetActiveUIWidgetOrNull<MenuBar.Widgets.GameTopMenuBar>()?.ActionButton;

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
            _actionsSystem.ActionsUpdated += OnActionsUpdated;
        }

        UpdateFilterLabel();
        QueueWindowUpdate();

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

        if (_playerManager.LocalEntity is not { } user)
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
            || action is { Charges: 0, RenewCharges: false }
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

        if (!_actionsSystem.ValidateWorldTarget(user, coords, (actionId, action)))
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
                action.Event.Action = actionId;
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

        if (!_actionsSystem.ValidateEntityTarget(user, entity, (actionId, action)))
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
                action.Event.Action = actionId;
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
            ActionButton.SetClickPressed(true);

        SearchAndDisplay();
    }

    private void OnWindowClosed()
    {
        if (ActionButton != null)
            ActionButton.SetClickPressed(false);
    }

    public void OnStateExited(GameplayState state)
    {
        if (_actionsSystem != null)
        {
            _actionsSystem.OnActionAdded -= OnActionAdded;
            _actionsSystem.OnActionRemoved -= OnActionRemoved;
            _actionsSystem.ActionsUpdated -= OnActionsUpdated;
        }

        CommandBinds.Unregister<ActionUIController>();
    }

    private void TriggerAction(int index)
    {
        if (_actionsSystem == null ||
            !_actions.TryGetValue(index, out var actionId) ||
            !_actionsSystem.TryGetActionData(actionId, out var baseAction))
        {
            return;
        }

        if (baseAction is BaseTargetActionComponent action)
            ToggleTargeting(actionId.Value, action);
        else
            _actionsSystem?.TriggerAction(actionId.Value, baseAction);
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

        if (_actions.Contains(actionId))
            return;

        _actions.Add(actionId);
    }

    private void OnActionRemoved(EntityUid actionId)
    {
        if (_container == null)
            return;

        if (actionId == SelectingTargetFor)
            StopTargeting();

        _actions.RemoveAll(x => x == actionId);
    }

    private void OnActionsUpdated()
    {
        QueueWindowUpdate();

        // TODO ACTIONS allow buttons to persist across state applications
        // Then we don't have to interrupt drags any time the buttons get rebuilt.
        _menuDragHelper.EndDrag();

        if (_actionsSystem != null)
            _container?.SetActionData(_actionsSystem, _actions.ToArray());
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
            Filters.Item => action.Container != null && action.Container != _playerManager.LocalEntity,
            Filters.Innate => action.Container == null || action.Container == _playerManager.LocalEntity,
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
        if (_window is not { Disposed: false, IsOpen: true })
            return;

        if (_actionsSystem == null)
            return;

        _window.UpdateNeeded = false;

        List<ActionButton> existing = new(_window.ResultsGrid.ChildCount);
        foreach (var child in _window.ResultsGrid.Children)
        {
            if (child is ActionButton button)
                existing.Add(button);
        }

        int i = 0;
        foreach (var action in actions)
        {
            if (i < existing.Count)
            {
                existing[i++].UpdateData(action.Id, _actionsSystem);
                continue;
            }

            var button = new ActionButton(_entMan, _spriteSystem, this) {Locked = true};
            button.ActionPressed += OnWindowActionPressed;
            button.ActionUnpressed += OnWindowActionUnPressed;
            button.ActionFocusExited += OnWindowActionFocusExisted;
            button.UpdateData(action.Id, _actionsSystem);
            _window.ResultsGrid.AddChild(button);
        }

        for (; i < existing.Count; i++)
        {
            existing[i].Dispose();
        }
    }

    public void QueueWindowUpdate()
    {
        if (_window != null)
            _window.UpdateNeeded = true;
    }

    private void SearchAndDisplay()
    {
        if (_window is not { Disposed: false, IsOpen: true })
            return;

        if (_actionsSystem == null)
            return;

        if (_playerManager.LocalEntity is not { } player)
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

    private void SetAction(ActionButton button, EntityUid? actionId, bool updateSlots = true)
    {
        if (_actionsSystem == null)
            return;

        int position;

        if (actionId == null)
        {
            button.ClearData();
            if (_container?.TryGetButtonIndex(button, out position) ?? false)
            {
                if (_actions.Count > position && position >= 0)
                    _actions.RemoveAt(position);
            }
        }
        else if (button.TryReplaceWith(actionId.Value, _actionsSystem) &&
            _container != null &&
            _container.TryGetButtonIndex(button, out position))
        {
            if (position >= _actions.Count)
            {
                _actions.Add(actionId);
            }
            else
            {
                _actions[position] = actionId;
            }
        }

        if (updateSlots)
            _container?.SetActionData(_actionsSystem, _actions.ToArray());
    }

    private void DragAction()
    {
        if (_menuDragHelper.Dragged is not {ActionId: {} action} dragged)
        {
            _menuDragHelper.EndDrag();
            return;
        }

        EntityUid? swapAction = null;
        var currentlyHovered = UIManager.MouseGetControl(_input.MouseScreenPosition);
        if (currentlyHovered is ActionButton button)
        {
            swapAction = button.ActionId;
            SetAction(button, action, false);
        }

        if (dragged.Parent is ActionButtonContainer)
            SetAction(dragged, swapAction, false);

        if (_actionsSystem != null)
            _container?.SetActionData(_actionsSystem, _actions.ToArray());

        _menuDragHelper.EndDrag();
    }

    private void OnClearPressed(ButtonEventArgs args)
    {
        if (_window == null)
            return;

        _window.SearchBar.Clear();
        _window.FilterButton.DeselectAll();
        UpdateFilterLabel();
        QueueWindowUpdate();
    }

    private void OnSearchChanged(LineEditEventArgs args)
    {
        QueueWindowUpdate();
    }

    private void OnFilterSelected(ItemPressedEventArgs args)
    {
        UpdateFilterLabel();
        QueueWindowUpdate();
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
        if (args.Function == EngineKeyFunctions.UIRightClick)
        {
            SetAction(button, null);
            args.Handle();
            return;
        }

        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        args.Handle();
        if (button.ActionId != null)
        {
            _menuDragHelper.MouseDown(button);
            return;
        }

        var ev = new FillActionSlotEvent();
        EntityManager.EventBus.RaiseEvent(EventSource.Local, ev);
        if (ev.Action != null)
            SetAction(button, ev.Action);
    }

    private void OnActionUnpressed(GUIBoundKeyEventArgs args, ActionButton button)
    {
        if (args.Function != EngineKeyFunctions.UIClick || _actionsSystem == null)
            return;

        args.Handle();

        if (_menuDragHelper.IsDragging)
        {
            DragAction();
            return;
        }

        _menuDragHelper.EndDrag();

        if (!_actionsSystem.TryGetActionData(button.ActionId, out var baseAction))
            return;

        if (baseAction is not BaseTargetActionComponent action)
        {
            _actionsSystem?.TriggerAction(button.ActionId.Value, baseAction);
            return;
        }

        // for target actions, we go into "select target" mode, we don't
        // message the server until we actually pick our target.

        // if we're clicking the same thing we're already targeting for, then we simply cancel
        // targeting
        ToggleTargeting(button.ActionId.Value, action);
    }

    private bool OnMenuBeginDrag()
    {
        // TODO ACTIONS
        // The dragging icon shuld be based on the entity's icon style. I.e. if the action has a large icon texture,
        // and a small item/provider sprite, then the dragged icon should be the big texture, not the provider.
        if (_actionsSystem != null && _actionsSystem.TryGetActionData(_menuDragHelper.Dragged?.ActionId, out var action))
        {
            if (EntityManager.TryGetComponent(action.EntityIcon, out SpriteComponent? sprite)
                && sprite.Icon?.GetFrame(RsiDirection.South, 0) is {} frame)
            {
                _dragShadow.Texture = frame;
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

        RegisterActionContainer(ActionsBar.ActionsContainer);

        _actionsSystem?.LinkAllActions();
    }

    public void RegisterActionContainer(ActionButtonContainer container)
    {
        if (_container != null)
        {
            _container.ActionPressed -= OnActionPressed;
            _container.ActionUnpressed -= OnActionUnpressed;
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
        if (_actionsSystem == null)
            return;

        _actions.Clear();
        foreach (var assign in assignments)
        {
            _actions.Add(assign.ActionId);
        }

        _container?.SetActionData(_actionsSystem, _actions.ToArray());
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
        if (_window is {UpdateNeeded: true})
            SearchAndDisplay();
    }

    private void OnComponentLinked(ActionsComponent component)
    {
        if (_actionsSystem == null)
            return;

        LoadDefaultActions();
        _container?.SetActionData(_actionsSystem, _actions.ToArray());
        QueueWindowUpdate();
    }

    private void OnComponentUnlinked()
    {
        _container?.ClearActionData();
        QueueWindowUpdate();
        StopTargeting();
    }

    private void LoadDefaultActions()
    {
        if (_actionsSystem == null)
            return;

        var actions = _actionsSystem.GetClientActions().Where(action => action.Comp.AutoPopulate).ToList();
        actions.Sort(ActionComparer);

        _actions.Clear();
        foreach (var (action, _) in actions)
        {
            if (!_actions.Contains(action))
                _actions.Add(action);
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
}
