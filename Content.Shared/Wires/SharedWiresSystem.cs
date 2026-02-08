using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.UserInterface;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Timing;

namespace Content.Shared.Wires;

/// <summary>
///     System that handles the wires on an entity. It is responsible for creating, updating, and destroying wires.
/// </summary>
public abstract class SharedWiresSystem : EntitySystem
{
    [Dependency] private readonly ActivatableUISystem _activatableUI = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;

    private static readonly ProtoId<ToolQualityPrototype> CuttingQuality = "Cutting";
    private static readonly ProtoId<ToolQualityPrototype> PulsingQuality = "Pulsing";

    // This is where all the wire layouts are stored.
    [ViewVariables] private readonly Dictionary<string, WireLayout> _layouts = [];

    private readonly float _toolTime = 0f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);

        // this is a broadcast event
        SubscribeLocalEvent<WiresComponent, PanelChangedEvent>(OnPanelChanged);
        SubscribeLocalEvent<WiresComponent, WiresActionMessage>(OnWiresActionMessage);
        SubscribeLocalEvent<WiresComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<WiresComponent, TimedWireEvent>(OnTimedWire);
        SubscribeLocalEvent<WiresComponent, PowerChangedEvent>(OnWiresPowered);
        SubscribeLocalEvent<WiresComponent, WireDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<WiresPanelSecurityComponent, WiresPanelSecurityEvent>(SetWiresPanelSecurity);

        SubscribeLocalEvent<WiresPanelComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<WiresPanelComponent, WirePanelDoAfterEvent>(OnPanelDoAfter);
        SubscribeLocalEvent<WiresPanelComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<WiresPanelComponent, ExaminedEvent>(OnExamine);

        SubscribeLocalEvent<ActivatableUIRequiresPanelComponent, ActivatableUIOpenAttemptEvent>(OnAttemptOpenActivatableUI);
        SubscribeLocalEvent<ActivatableUIRequiresPanelComponent, PanelChangedEvent>(OnActivatableUIPanelChanged);
    }

    private void OnStartup(Entity<WiresPanelComponent> ent, ref ComponentStartup args)
    {
        UpdateAppearance(ent);
    }

    private void OnPanelDoAfter(Entity<WiresPanelComponent> ent, ref WirePanelDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TogglePanel(ent, !ent.Comp.Open, args.User))
            return;

        _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.User):user} screwed {ToPrettyString(ent):target}'s maintenance panel {(ent.Comp.Open ? "open" : "closed")}");

        var sound = ent.Comp.Open ? ent.Comp.ScrewdriverOpenSound : ent.Comp.ScrewdriverCloseSound;
        _audio.PlayPredicted(sound, ent.Owner, args.User);
        args.Handled = true;
    }

    private void OnInteractUsing(Entity<WiresPanelComponent> ent, ref InteractUsingEvent args)
    {
        if (!_tool.HasQuality(args.Used, ent.Comp.OpeningTool))
            return;

        if (!CanTogglePanel(ent, args.User))
            return;

        if (!_tool.UseTool(
                args.Used,
                args.User,
                ent,
                (float)ent.Comp.OpenDelay.TotalSeconds,
                ent.Comp.OpeningTool,
                new WirePanelDoAfterEvent()))
        {
            return;
        }

        _adminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(args.User):user} is screwing {ToPrettyString(ent):target}'s {(ent.Comp.Open ? "open" : "closed")} maintenance panel at {Transform(ent).Coordinates:targetlocation}");
        args.Handled = true;
    }

    private void OnExamine(Entity<WiresPanelComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(WiresPanelComponent)))
        {
            if (!ent.Comp.Open)
            {
                if (!string.IsNullOrEmpty(ent.Comp.ExamineTextClosed))
                    args.PushMarkup(Loc.GetString(ent.Comp.ExamineTextClosed));
            }
            else
            {
                if (!string.IsNullOrEmpty(ent.Comp.ExamineTextOpen))
                    args.PushMarkup(Loc.GetString(ent.Comp.ExamineTextOpen));

                if (TryComp<WiresPanelSecurityComponent>(ent.Owner, out var wiresPanelSecurity) &&
                    wiresPanelSecurity.Examine != null)
                {
                    args.PushMarkup(Loc.GetString(wiresPanelSecurity.Examine));
                }
            }
        }
    }

    public void ChangePanelVisibility(Entity<WiresPanelComponent> ent, bool visible)
    {
        ent.Comp.Visible = visible;
        UpdateAppearance(ent);
        Dirty(ent);
    }

    protected void UpdateAppearance(Entity<WiresPanelComponent> ent)
    {
        if (TryComp<AppearanceComponent>(ent.Owner, out var appearance))
            _appearance.SetData(ent.Owner, WiresVisuals.MaintenancePanelState, ent.Comp is { Open: true, Visible: true }, appearance);
    }

    public bool TogglePanel(Entity<WiresPanelComponent> ent, bool open, EntityUid? user = null)
    {
        if (!CanTogglePanel(ent, user))
            return false;

        ent.Comp.Open = open;
        UpdateAppearance(ent);
        Dirty(ent.Owner, ent.Comp);

        var ev = new PanelChangedEvent(ent.Comp.Open);
        RaiseLocalEvent(ent.Owner, ref ev);
        return true;
    }

    public bool CanTogglePanel(Entity<WiresPanelComponent> ent, EntityUid? user)
    {
        var attempt = new AttemptChangePanelEvent(ent.Comp.Open, user);
        RaiseLocalEvent(ent, ref attempt);
        return !attempt.Cancelled;
    }

    public bool IsPanelOpen(Entity<WiresPanelComponent?> ent, EntityUid? tool = null)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return true;

        if (tool != null)
        {
            var ev = new PanelOverrideEvent();
            RaiseLocalEvent(tool.Value, ref ev);

            if (ev.Allowed)
                return true;
        }

        // Listen, i don't know what the fuck this component does. it's stapled on shit for airlocks
        // but it looks like an almost direct duplication of WiresPanelComponent except with a shittier API.
        if (TryComp<WiresPanelSecurityComponent>(ent, out var wiresPanelSecurity) &&
            !wiresPanelSecurity.WiresAccessible)
            return false;

        return ent.Comp.Open;
    }

    private void OnAttemptOpenActivatableUI(Entity<ActivatableUIRequiresPanelComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled || !TryComp<WiresPanelComponent>(ent, out var wires))
            return;

        if (ent.Comp.RequireOpen != wires.Open)
            args.Cancel();
    }

    private void OnActivatableUIPanelChanged(Entity<ActivatableUIRequiresPanelComponent> ent, ref PanelChangedEvent args)
    {
        if (args.Open == ent.Comp.RequireOpen)
            return;

        _activatableUI.CloseAll(ent.Owner);
    }

    #region DoAfters
    private void OnTimedWire(Entity<WiresComponent> ent, ref TimedWireEvent args)
    {
        args.Delegate(args.Wire);
        UpdateUserInterface(ent.Owner);
    }

    /// <summary>
    ///     Tries to cancel an active wire action via the given key that it's stored in.
    /// </summary>
    /// <param name="owner">The entity that the action is stored in.</param>
    /// <param name="key">The key used to cancel the action.</param>
    public bool TryCancelWireAction(EntityUid owner, object key)
    {
        if (!_activeWireActions.TryGetValue(owner, out var actions))
            return false;

        if (!actions.TryGetValue(key, out var action))
            return false;

        action.Cancelled = true;
        return true;
    }

    /// <summary>
    ///     Starts a timed action for this entity. Actions are keyed so callers can cancel or prevent duplicates.
    /// </summary>
    /// <param name="owner">The entity that the action is stored in.</param>
    /// <param name="delay">How long this takes to finish.</param>
    /// <param name="key">The key used to cancel the action.</param>
    /// <param name="onFinish">The event that is sent out when the wire is finished <see cref="TimedWireEvent" />.</param>
    public void StartWireAction(EntityUid owner, float delay, object key, TimedWireEvent onFinish)
    {
        if (!HasComp<WiresComponent>(owner))
        {
            return;
        }

        if (!_activeWireActions.TryGetValue(owner, out var actions))
        {
            actions = [];
            _activeWireActions[owner] = actions;
        }

        if (actions.ContainsKey(key))
        {
            return;
        }

        var endTime = _timing.CurTime + TimeSpan.FromSeconds(delay);

        actions.Add(key,
            new ActiveWireAction (
            key,
            endTime,
            onFinish
        ));

        UpdateUserInterface(owner);
    }

    private readonly Dictionary<EntityUid, Dictionary<object, ActiveWireAction>> _activeWireActions = [];
    private readonly List<(EntityUid Owner, object Key)> _finishedWireActions = [];
    private readonly List<EntityUid> _ownersToRemove = [];

    public override void Update(float frameTime)
    {
        if (_activeWireActions.Count == 0)
            return;

        var now = _timing.CurTime;

        foreach (var (owner, actions) in _activeWireActions.ToArray())
        {
            if (!HasComp<WiresComponent>(owner))
            {
                _ownersToRemove.Add(owner);
                continue;
            }

            foreach (var (key, action) in actions.ToArray())
            {
                if (action.Cancelled || now >= action.EndTime)
                {
                    RaiseLocalEvent(owner, action.OnFinish, true);
                    _finishedWireActions.Add((owner, key));
                }
            }
        }

        if (_finishedWireActions.Count != 0)
        {
            foreach (var (owner, key) in _finishedWireActions)
            {
                if (!_activeWireActions.TryGetValue(owner, out var activeActions))
                {
                    continue;
                }

                if (!activeActions.Remove(key))
                {
                    continue;
                }

                RemoveData(owner, key);
                UpdateUserInterface(owner);

                if (activeActions.Count == 0)
                {
                    _ownersToRemove.Add(owner);
                }
            }

            _finishedWireActions.Clear();
        }

        if (_ownersToRemove.Count != 0)
        {
            foreach (var owner in _ownersToRemove)
            {
                _activeWireActions.Remove(owner);
            }

            _ownersToRemove.Clear();
        }
    }

    private sealed class ActiveWireAction(object id, TimeSpan endTime, TimedWireEvent onFinish)
    {
        /// <summary>
        ///     The wire action's ID. This is so that once the action is finished,
        ///     any related data can be removed from the state dictionary.
        /// </summary>
        public object Id { get; } = id;

        /// <summary>
        ///     When this action should fire.
        /// </summary>
        public TimeSpan EndTime { get; } = endTime;

        /// <summary>
        ///     Whether this action was cancelled by the owning system.
        /// </summary>
        public bool Cancelled { get; set; }

        /// <summary>
        ///     The event called once the action finishes.
        /// </summary>
        public TimedWireEvent OnFinish { get; } = onFinish;
    }

    #endregion

    #region Event Handling
    private void OnWiresPowered(Entity<WiresComponent> ent, ref PowerChangedEvent args)
    {
        UpdateUserInterface(ent.Owner);
        foreach (var wire in ent.Comp.WiresList)
        {
            wire.Action?.Update(wire);
        }
    }

    private void OnWiresActionMessage(Entity<WiresComponent> ent, ref WiresActionMessage args)
    {
        var player = args.Actor;

        if (!TryComp(player, out HandsComponent? handsComponent))
        {
            _popup.PopupClient(Loc.GetString("wires-component-ui-on-receive-message-no-hands"), ent.Owner, player);
            return;
        }

        if (!_interaction.InRangeUnobstructed(player, ent.Owner))
        {
            _popup.PopupClient(Loc.GetString("wires-component-ui-on-receive-message-cannot-reach"), ent.Owner, player);
            return;
        }

        if (!_hands.TryGetActiveItem((player, handsComponent), out var heldEntity))
            return;

        if (!TryComp(heldEntity, out ToolComponent? tool))
            return;

        TryDoWireAction(ent.AsNullable(), player, heldEntity.Value, args.Id, args.Action, tool);
    }

    private void OnDoAfter(Entity<WiresComponent> ent, ref WireDoAfterEvent args)
    {
        if (args.Cancelled)
        {
            ent.Comp.WiresQueue.Remove(args.Id);
            Dirty(ent);
            return;
        }

        if (args.Handled || args.Args.Target == null || args.Args.Used == null)
            return;

        UpdateWires((args.Args.Target.Value, ent.Comp), args.Args.User, args.Args.Used.Value, args.Id, args.Action);

        args.Handled = true;
    }

    private void OnInteractUsing(Entity<WiresComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<ToolComponent>(args.Used, out var tool))
            return;

        if (!IsPanelOpen(ent.Owner))
            return;

        if (_tool.HasQuality(args.Used, CuttingQuality, tool) ||
            _tool.HasQuality(args.Used, PulsingQuality, tool))
        {
            if (TryComp(args.User, out ActorComponent? actor))
            {
                _uiSystem.OpenUi(ent.Owner, WiresUiKey.Key, actor.PlayerSession);
                args.Handled = true;
            }
        }
    }

    private void OnPanelChanged(Entity<WiresComponent> ent, ref PanelChangedEvent args)
    {
        if (args.Open)
            return;

        _uiSystem.CloseUi(ent.Owner, WiresUiKey.Key);
    }
    #endregion

    #region Entity API
    protected void UpdateUserInterface(Entity<WiresComponent?> ent, UserInterfaceComponent? ui = null)
    {
        // TODO: need to predict WiresBoundUserInterface correctly
        if (_net.IsClient)
            return;

        if (!Resolve(ent, ref ent.Comp, ref ui, false)) // logging this means that we get a bunch of errors
            return;

        var clientList = new List<ClientWire>();
        foreach (var entry in ent.Comp.WiresList)
        {
            clientList.Add(new ClientWire(entry.Id, entry.IsCut, entry.Color, entry.Letter));

            var statusData = entry.Action?.GetStatusLightData(entry);
            if (statusData != null && entry.Action?.StatusKey != null)
            {
                ent.Comp.Statuses[entry.Action.StatusKey] = (entry.OriginalPosition, statusData);
            }
        }

        var statuses = new List<(int position, object key, object value)>();
        foreach (var (key, value) in ent.Comp.Statuses)
        {
            var valueCast = ((int position, StatusLightData? value))value;
            statuses.Add((valueCast.position, key, valueCast.value!));
        }

        statuses.Sort((a, b) => a.position.CompareTo(b.position));

        _uiSystem.SetUiState((ent.Owner, ui),
            WiresUiKey.Key,
            new WiresBoundUserInterfaceState(
            [.. clientList],
            statuses.Select(p => new StatusEntry(p.key, p.value)).ToArray(),
            Loc.GetString(ent.Comp.BoardName),
            ent.Comp.SerialNumber,
            ent.Comp.WireSeed));
    }

    public void OpenUserInterface(EntityUid uid, ICommonSession player)
    {
        _uiSystem.OpenUi(uid, WiresUiKey.Key, player);
    }

    /// <summary>
    ///     Tries to get a wire on this entity by its integer id.
    /// </summary>
    /// <returns>The wire if found, otherwise null</returns>
    public Wire? TryGetWire(Entity<WiresComponent?> ent, int id)
    {
        if (!Resolve(ent, ref ent.Comp))
            return null;

        return id >= 0 && id < ent.Comp.WiresList.Count
            ? ent.Comp.WiresList[id]
            : null;
    }

    /// <summary>
    ///     Tries to get all the wires on this entity by the wire action type.
    /// </summary>
    /// <returns>Enumerator of all wires in this entity according to the given type.</returns>
    public IEnumerable<Wire> TryGetWires<T>(EntityUid uid, WiresComponent? wires = null) where T : IWireAction
    {
        if (!Resolve(uid, ref wires))
            yield break;

        foreach (var wire in wires.WiresList)
        {
            if (wire.Action?.GetType() == typeof(T))
            {
                yield return wire;
            }
        }
    }

    public void SetWiresPanelSecurity(EntityUid uid, WiresPanelSecurityComponent component, WiresPanelSecurityEvent args)
    {
        component.Examine = args.Examine;
        component.WiresAccessible = args.WiresAccessible;

        Dirty(uid, component);

        if (!args.WiresAccessible)
        {
            _uiSystem.CloseUi(uid, WiresUiKey.Key);
        }
    }

    private void TryDoWireAction(Entity<WiresComponent?> ent, EntityUid user, EntityUid toolEntity, int id, WiresAction action, ToolComponent? tool = null)
    {
        if (!Resolve(ent, ref ent.Comp, false)
            || !Resolve(toolEntity, ref tool))
            return;

        if (ent.Comp.WiresQueue.Contains(id))
            return;

        var wire = TryGetWire(ent, id);

        if (wire == null)
            return;

        switch (action)
        {
            case WiresAction.Cut:
                if (!_tool.HasQuality(toolEntity, CuttingQuality, tool))
                {
                    _popup.PopupPredictedCursor(Loc.GetString("wires-component-ui-on-receive-message-need-wirecutters"), user);
                    return;
                }

                if (wire.IsCut)
                {
                    _popup.PopupPredictedCursor(Loc.GetString("wires-component-ui-on-receive-message-cannot-cut-cut-wire"), user);
                    return;
                }

                break;
            case WiresAction.Mend:
                if (!_tool.HasQuality(toolEntity, CuttingQuality, tool))
                {
                    _popup.PopupPredictedCursor(Loc.GetString("wires-component-ui-on-receive-message-need-wirecutters"), user);
                    return;
                }

                if (!wire.IsCut)
                {
                    _popup.PopupPredictedCursor(Loc.GetString("wires-component-ui-on-receive-message-cannot-mend-uncut-wire"), user);
                    return;
                }

                break;
            case WiresAction.Pulse:
                if (!_tool.HasQuality(toolEntity, PulsingQuality, tool))
                {
                    _popup.PopupPredictedCursor(Loc.GetString("wires-component-ui-on-receive-message-need-multitool"), user);
                    return;
                }

                if (wire.IsCut)
                {
                    _popup.PopupPredictedCursor(Loc.GetString("wires-component-ui-on-receive-message-cannot-pulse-cut-wire"), user);
                    return;
                }

                break;
        }

        ent.Comp.WiresQueue.Add(id);
        Dirty(ent);

        if (_toolTime > 0f)
        {
            var args = new DoAfterArgs(EntityManager, user, _toolTime, new WireDoAfterEvent(action, id), ent.Owner, target: ent.Owner, used: toolEntity)
            {
                NeedHand = true,
                BreakOnDamage = true,
                BreakOnMove = true
            };

            _doAfter.TryStartDoAfter(args);
        }
        else
        {
            UpdateWires(ent, user, toolEntity, id, action);
        }
    }

    private void UpdateWires(Entity<WiresComponent?> ent, EntityUid user, EntityUid toolEntity, int id, WiresAction action, ToolComponent? tool = null)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (!ent.Comp.WiresQueue.Contains(id))
            return;

        if (!Resolve(toolEntity, ref tool))
        {
            ent.Comp.WiresQueue.Remove(id);
            Dirty(ent);
            return;
        }

        var wire = TryGetWire(ent, id);

        if (wire == null)
        {
            ent.Comp.WiresQueue.Remove(id);
            Dirty(ent);
            return;
        }

        switch (action)
        {
            case WiresAction.Cut:
                if (!_tool.HasQuality(toolEntity, CuttingQuality, tool))
                {
                    _popup.PopupPredictedCursor(Loc.GetString("wires-component-ui-on-receive-message-need-wirecutters"), user);
                    break;
                }

                if (wire.IsCut)
                {
                    _popup.PopupPredictedCursor(Loc.GetString("wires-component-ui-on-receive-message-cannot-cut-cut-wire"), user);
                    break;
                }

                _tool.PlayToolSound(toolEntity, tool, null);
                if (wire.Action == null || wire.Action.Cut(user, wire))
                {
                    wire.IsCut = true;
                }

                UpdateUserInterface(ent.Owner);
                break;
            case WiresAction.Mend:
                if (!_tool.HasQuality(toolEntity, CuttingQuality, tool))
                {
                    _popup.PopupPredictedCursor(Loc.GetString("wires-component-ui-on-receive-message-need-wirecutters"), user);
                    break;
                }

                if (!wire.IsCut)
                {
                    _popup.PopupPredictedCursor(Loc.GetString("wires-component-ui-on-receive-message-cannot-mend-uncut-wire"), user);
                    break;
                }

                _tool.PlayToolSound(toolEntity, tool, null);
                if (wire.Action == null || wire.Action.Mend(user, wire))
                {
                    wire.IsCut = false;
                }

                UpdateUserInterface(ent.Owner);
                break;
            case WiresAction.Pulse:
                if (!_tool.HasQuality(toolEntity, PulsingQuality, tool))
                {
                    _popup.PopupPredictedCursor(Loc.GetString("wires-component-ui-on-receive-message-need-multitool"), user);
                    break;
                }

                if (wire.IsCut)
                {
                    _popup.PopupPredictedCursor(Loc.GetString("wires-component-ui-on-receive-message-cannot-pulse-cut-wire"), user);
                    break;
                }

                wire.Action?.Pulse(user, wire);

                UpdateUserInterface(ent.Owner);
                _audio.PlayPredicted(ent.Comp.PulseSound, ent.Owner, user);
                break;
        }

        wire.Action?.Update(wire);
        ent.Comp.WiresQueue.Remove(id);
        Dirty(ent);
    }

    /// <summary>
    ///     Tries to get the stateful data stored in this entity's <see cref="WiresComponent"/>.
    /// </summary>
    /// <param name="uid">The entity to get the data from.</param>
    /// <param name="identifier">The key that stores the data in the WiresComponent.</param>
    /// <param name="data">The data to store using the given identifier.</param>
    /// <param name="wires">The wires component to get the data from.</param>
    public bool TryGetData<T>(EntityUid uid, object identifier, [NotNullWhen(true)] out T? data, WiresComponent? wires = null)
    {
        data = default;
        if (!Resolve(uid, ref wires))
            return false;

        wires.StateData.TryGetValue(identifier, out var result);

        if (result is not T value)
            return false;

        data = value;

        return true;
    }

    /// <summary>
    ///     Sets data in the entity's WiresComponent state dictionary by key.
    /// </summary>
    /// <param name="ent">The entity.</param>
    /// <param name="identifier">The key that stores the data in the WiresComponent.</param>
    /// <param name="data">The data to store using the given identifier.</param>
    public void SetData(Entity<WiresComponent?> ent, object identifier, object data)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.StateData.TryGetValue(identifier, out var storedMessage))
        {
            if (storedMessage == data)
            {
                return;
            }
        }

        ent.Comp.StateData[identifier] = data;
        UpdateUserInterface(ent.Owner);
    }

    /// <summary>
    ///     If this entity has data stored via this key in the WiresComponent it has.
    /// </summary>
    public bool HasData(Entity<WiresComponent?> ent, object identifier)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        return ent.Comp.StateData.ContainsKey(identifier);
    }

    /// <summary>
    ///     Removes data from this entity stored in the given key from the entity's <see cref="WiresComponent"/>.
    /// </summary>
    /// <param name="ent">The entity.</param>
    /// <param name="identifier">The key that stores the data in the WiresComponent.</param>
    public void RemoveData(Entity<WiresComponent?> ent, object identifier)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.StateData.Remove(identifier);
    }
    #endregion

    #region Layout Handling
    protected bool TryGetLayout(string id, [NotNullWhen(true)] out WireLayout? layout)
    {
        return _layouts.TryGetValue(id, out layout);
    }

    protected void AddLayout(string id, WireLayout layout)
    {
        _layouts.Add(id, layout);
    }

    protected void Reset(RoundRestartCleanupEvent args)
    {
        _layouts.Clear();
    }
    #endregion
}

[Serializable, NetSerializable]
public sealed class Wire(EntityUid owner, bool isCut, WireColor color, WireLetter letter, int position, IWireAction? action)
{
    /// <summary>
    /// The entity that registered the wire.
    /// </summary>
    [field: NonSerialized]
    public EntityUid Owner { get; } = owner;

    /// <summary>
    /// Whether the wire is cut.
    /// </summary>
    public bool IsCut { get; set; } = isCut;

    /// <summary>
    /// Used in client-server communication to identify a wire without telling the client what the wire does.
    /// </summary>
    [ViewVariables]
    public int Id { get; set; }

    /// <summary>
    /// The original position of this wire in the prototype.
    /// </summary>
    [ViewVariables]
    public int OriginalPosition { get; set; } = position;

    /// <summary>
    /// The color of the wire.
    /// </summary>
    [ViewVariables]
    public WireColor Color { get; } = color;

    /// <summary>
    /// The greek letter shown below the wire.
    /// </summary>
    [ViewVariables]
    public WireLetter Letter { get; } = letter;

    /// <summary>
    ///     The action that this wire performs when mended, cut or puled. This also determines the status lights that this wire adds.
    /// </summary>
    [field: NonSerialized]
    public IWireAction? Action { get; set; } = action;
}

// this is here so that when a DoAfter event is called,
// WiresSystem can call the action in question after the
// doafter is finished (either through cancellation
// or completion - this is implementation dependent)
public delegate void WireActionDelegate(Wire wire);

// callbacks over the event bus,
// because async is banned
public sealed class TimedWireEvent(WireActionDelegate @delegate, Wire wire) : EntityEventArgs
{
    /// <summary>
    ///     The function to be called once
    ///     the timed event is complete.
    /// </summary>
    public WireActionDelegate Delegate { get; } = @delegate;

    /// <summary>
    ///     The wire tied to this timed wire event.
    /// </summary>
    public Wire Wire { get; } = wire;
}

public sealed class WireLayout(IReadOnlyDictionary<int, WireLayout.WireData> specifications)
{
    // why is this an <int, WireData>?
    // List<T>.Insert panics,
    // and I needed a uniquer key for wires
    // which allows me to have a unified identifier
    [ViewVariables] public IReadOnlyDictionary<int, WireData> Specifications { get; } = specifications;

    public sealed class WireData(WireLetter letter, WireColor color, int position)
    {
        public WireLetter Letter { get; } = letter;
        public WireColor Color { get; } = color;
        public int Position { get; } = position;
    }
}

/// <summary>
/// Raised directed on a tool to try and override panel visibility.
/// </summary>
[ByRefEvent]
public record struct PanelOverrideEvent()
{
    public bool Allowed = true;
}
