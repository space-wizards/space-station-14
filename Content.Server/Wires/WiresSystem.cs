using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Content.Server.DoAfter;
using Content.Server.Hands.Components;
using Content.Server.Power.Components;
using Content.Server.Tools;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools.Components;
using Content.Shared.Wires;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Wires;

public sealed class WiresSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly ToolSystem _toolSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;

    private IRobustRandom _random = new RobustRandom();

    // This is where all the wire layouts are stored.
    [ViewVariables] private readonly Dictionary<string, WireLayout> _layouts = new();

    private const float ScrewTime = 1f;
    private float _toolTime = 0f;

    private static DummyWireAction _dummyWire = new DummyWireAction();

    #region Initialization
    public override void Initialize()
    {
        _dummyWire.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);

        // this is a broadcast event
        SubscribeLocalEvent<WireToolFinishedEvent>(OnToolFinished);
        SubscribeLocalEvent<WiresComponent, ComponentStartup>(OnWiresStartup);
        SubscribeLocalEvent<WiresComponent, WiresActionMessage>(OnWiresActionMessage);
        SubscribeLocalEvent<WiresComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<WiresComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<WiresComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<WiresComponent, TimedWireEvent>(OnTimedWire);
        SubscribeLocalEvent<WiresComponent, PowerChangedEvent>(OnWiresPowered);
        SubscribeLocalEvent<WiresComponent, OnWireDoAfterEvent>(OnWireDoAfter);
        SubscribeLocalEvent<WiresComponent, OnWireDoAfterCancelEvent>(OnWireDoAfterCancel);
    }

    private void SetOrCreateWireLayout(EntityUid uid, WiresComponent? wires = null)
    {
        if (!Resolve(uid, ref wires))
            return;

        WireLayout? layout = null;
        List<Wire>? wireSet = null;
        if (wires.LayoutId != null)
        {
            if (!wires.AlwaysRandomize)
            {
                TryGetLayout(wires.LayoutId, out layout);
            }

            if (!_protoMan.TryIndex(wires.LayoutId, out WireLayoutPrototype? layoutPrototype))
                return;

            // does the prototype have a parent (and are the wires empty?) if so, we just create
            // a new layout based on that
            //
            // TODO: Merge wire layouts...
            if (!string.IsNullOrEmpty(layoutPrototype.Parent) && layoutPrototype.Wires == null)
            {
                var parent = layoutPrototype.Parent;

                if (!_protoMan.TryIndex(parent, out WireLayoutPrototype? parentPrototype))
                    return;

                layoutPrototype = parentPrototype;
            }

            if (layoutPrototype.Wires != null)
            {
                foreach (var wire in layoutPrototype.Wires)
                {
                    wire.Initialize();
                }

                wireSet = CreateWireSet(uid, layout, layoutPrototype.Wires, layoutPrototype.DummyWires);
            }
        }

        if (wireSet == null || wireSet.Count == 0)
        {
            return;
        }

        wires.WiresList.AddRange(wireSet);

        Dictionary<object, int> types = new Dictionary<object, int>();

        if (layout != null)
        {
            for (var i = 0; i < wireSet.Count; i++)
            {
                wires.WiresList[layout.Specifications[i].Position] = wireSet[i];
            }

            var id = 0;
            foreach (var wire in wires.WiresList)
            {
                var wireType = wire.Action.GetType();
                if (types.ContainsKey(wireType))
                {
                    types[wireType] += 1;
                }
                else
                {
                    types.Add(wireType, 1);
                }

                wire.Id = id;
                id++;

                // don't care about the result, this should've
                // been handled in layout creation
                wire.Action.AddWire(wire, types[wireType]);
            }
        }
        else
        {
            var enumeratedList = new List<(int, Wire)>();
            var data = new Dictionary<int, WireLayout.WireData>();
            for (int i = 0; i < wireSet.Count; i++)
            {
                enumeratedList.Add((i, wireSet[i]));
            }
            _random.Shuffle(enumeratedList);

            for (var i = 0; i < enumeratedList.Count; i++)
            {
                (int id, Wire d) = enumeratedList[i];

                var wireType = d.Action.GetType();
                if (types.ContainsKey(wireType))
                {
                    types[wireType] += 1;
                }
                else
                {
                    types.Add(wireType, 1);
                }

                d.Id = i;

                if (!d.Action.AddWire(d, types[wireType]))
                {
                    d.Action = _dummyWire;
                }

                data.Add(id, new WireLayout.WireData(d.Letter, d.Color, i));

                wires.WiresList[i] = wireSet[id];
            }

            if (!wires.AlwaysRandomize && !string.IsNullOrEmpty(wires.LayoutId))
            {
                AddLayout(wires.LayoutId, new WireLayout(data));
            }
        }
    }

    private List<Wire>? CreateWireSet(EntityUid uid, WireLayout? layout, List<IWireAction> wires, int dummyWires)
    {
        if (wires.Count == 0)
            return null;

        List<WireColor> colors =
            new((WireColor[]) Enum.GetValues(typeof(WireColor)));

        List<WireLetter> letters =
            new((WireLetter[]) Enum.GetValues(typeof(WireLetter)));


        var wireSet = new List<Wire>();
        for (var i = 0; i < wires.Count; i++)
        {
            wireSet.Add(CreateWire(uid, wires[i], i, layout, colors, letters));
        }

        for (var i = 1; i <= dummyWires; i++)
        {
            wireSet.Add(CreateWire(uid, _dummyWire, wires.Count + i, layout, colors, letters));
        }

        return wireSet;
    }

    private Wire CreateWire(EntityUid uid, IWireAction action, int position, WireLayout? layout, List<WireColor> colors, List<WireLetter> letters)
    {
        WireLetter letter;
        WireColor color;

        if (layout != null
            && layout.Specifications.TryGetValue(position, out var spec))
        {
            color = spec.Color;
            letter = spec.Letter;
            colors.Remove(color);
            letters.Remove(letter);
        }
        else
        {
            color = colors.Count == 0 ? WireColor.Red : _random.PickAndTake(colors);
            letter = letters.Count == 0 ? WireLetter.α : _random.PickAndTake(letters);
        }

        return new Wire(
            uid,
            false,
            color,
            letter,
            action);
    }

    private void OnWiresStartup(EntityUid uid, WiresComponent component, ComponentStartup args)
    {
        if (!String.IsNullOrEmpty(component.LayoutId))
            SetOrCreateWireLayout(uid, component);

        UpdateUserInterface(uid);
        UpdateAppearance(uid);
    }
    #endregion

    #region DoAfters
    private void OnTimedWire(EntityUid uid, WiresComponent component, TimedWireEvent args)
    {
        args.Delegate(args.Wire);
        UpdateUserInterface(uid);
    }

    /// <summary>
    ///     Tries to cancel an active wire action via the given key that it's stored in.
    /// </summary>
    /// <param id="key">The key used to cancel the action.</param>
    public bool TryCancelWireAction(EntityUid owner, object key)
    {
        if (TryGetData(owner, key, out CancellationTokenSource? token))
        {
            token.Cancel();
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Starts a timed action for this entity.
    /// </summary>
    /// <param id="delay">How long this takes to finish</param>
    /// <param id="key">The key used to cancel the action</param>
    /// <param id="onFinish">The event that is sent out when the wire is finished <see cref="TimedWireEvent" /></param>
    public void StartWireAction(EntityUid owner, float delay, object key, TimedWireEvent onFinish)
    {
        if (!HasComp<WiresComponent>(owner))
        {
            return;
        }

        if (!_activeWires.ContainsKey(owner))
        {
            _activeWires.Add(owner, new());
        }

        CancellationTokenSource tokenSource = new();

        // Starting an already started action will do nothing.
        if (HasData(owner, key))
        {
            return;
        }

        SetData(owner, key, tokenSource);

        _activeWires[owner].Add(new ActiveWireAction
        (
            key,
            delay,
            tokenSource.Token,
            onFinish
        ));
    }

    private Dictionary<EntityUid, List<ActiveWireAction>> _activeWires = new();
    private List<(EntityUid, ActiveWireAction)> _finishedWires = new();

    public override void Update(float frameTime)
    {
        foreach (var (owner, activeWires) in _activeWires)
        {
            foreach (var wire in activeWires)
            {
                if (wire.CancelToken.IsCancellationRequested)
                {
                    RaiseLocalEvent(owner, wire.OnFinish);
                    _finishedWires.Add((owner, wire));
                }
                else
                {
                    wire.TimeLeft -= frameTime;
                    if (wire.TimeLeft <= 0)
                    {
                        RaiseLocalEvent(owner, wire.OnFinish);
                        _finishedWires.Add((owner, wire));
                    }
                }
            }
        }

        if (_finishedWires.Count != 0)
        {
            foreach (var (owner, wireAction) in _finishedWires)
            {
                // sure
                _activeWires[owner].RemoveAll(action => action.CancelToken == wireAction.CancelToken);

                if (_activeWires[owner].Count == 0)
                {
                    _activeWires.Remove(owner);
                }

                RemoveData(owner, wireAction.Id);
            }

            _finishedWires.Clear();
        }
    }

    private class ActiveWireAction
    {
        /// <summary>
        ///     The wire action's ID. This is so that once the action is finished,
        ///     any related data can be removed from the state dictionary.
        /// </summary>
        public object Id;

        /// <summary>
        ///     How much time is left in this action before it finishes.
        /// </summary>
        public float TimeLeft;

        /// <summary>
        ///     The token used to cancel the action.
        /// </summary>
        public CancellationToken CancelToken;

        /// <summary>
        ///     The event called once the action finishes.
        /// </summary>
        public TimedWireEvent OnFinish;

        public ActiveWireAction(object identifier, float time, CancellationToken cancelToken, TimedWireEvent onFinish)
        {
            Id = identifier;
            TimeLeft = time;
            CancelToken = cancelToken;
            OnFinish = onFinish;
        }
    }

    #endregion

    #region Event Handling
    private void OnWiresPowered(EntityUid uid, WiresComponent component, PowerChangedEvent args)
    {
        UpdateUserInterface(uid);
        foreach (var wire in component.WiresList)
        {
            wire.Action.Update(wire);
        }
    }

    private void OnWiresActionMessage(EntityUid uid, WiresComponent component, WiresActionMessage args)
    {
        if (args.Session.AttachedEntity == null)
        {
            return;
        }
        var player = (EntityUid) args.Session.AttachedEntity;

        if (!EntityManager.TryGetComponent(player, out HandsComponent? handsComponent))
        {
            _popupSystem.PopupEntity(Loc.GetString("wires-component-ui-on-receive-message-no-hands"), uid, Filter.Entities(player));
            return;
        }

        if (!_interactionSystem.InRangeUnobstructed(player, uid))
        {
            _popupSystem.PopupEntity(Loc.GetString("wires-component-ui-on-receive-message-cannot-reach"), uid, Filter.Entities(player));
            return;
        }

        var activeHand = handsComponent.ActiveHand;

        if (activeHand == null)
            return;

        if (activeHand.HeldEntity == null)
            return;

        var activeHandEntity = activeHand.HeldEntity.Value;
        if (!EntityManager.TryGetComponent(activeHandEntity, out ToolComponent? tool))
            return;

        TryDoWireAction(uid, player, activeHandEntity, args.Id, args.Action, component, tool);
    }

    private void OnWireDoAfter(EntityUid uid, WiresComponent component, OnWireDoAfterEvent args)
    {
        UpdateWires(args.Target, args.User, args.Tool, args.Id, args.Action, component);
    }

    private void OnWireDoAfterCancel(EntityUid uid, WiresComponent component, OnWireDoAfterCancelEvent args)
    {
        component.WiresQueue.Remove(args.Id);
    }

    private void OnInteractUsing(EntityUid uid, WiresComponent component, InteractUsingEvent args)
    {
        if (!EntityManager.TryGetComponent(args.Used, out ToolComponent? tool))
            return;

        if (component.IsPanelOpen &&
            (_toolSystem.HasQuality(args.Used, "Cutting", tool) ||
            _toolSystem.HasQuality(args.Used, "Pulsing", tool)))
        {
            if (EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
            {
                _uiSystem.GetUiOrNull(uid, WiresUiKey.Key)?.Open(actor.PlayerSession);
                args.Handled = true;
            }
        }
        else if (_toolSystem.UseTool(args.Used, args.User, uid, 0f, ScrewTime, new string[]{ "Screwing" }, doAfterCompleteEvent:new WireToolFinishedEvent(uid), toolComponent:tool))
        {
            args.Handled = true;
        }
    }

    private void OnToolFinished(WireToolFinishedEvent args)
    {
        if (!EntityManager.TryGetComponent(args.Target, out WiresComponent? component))
            return;

        component.IsPanelOpen = !component.IsPanelOpen;
        UpdateAppearance(args.Target);

        if (component.IsPanelOpen)
        {
            SoundSystem.Play(component.ScrewdriverOpenSound.GetSound(), Filter.Pvs(args.Target), args.Target);
        }
        else
        {
            SoundSystem.Play(component.ScrewdriverCloseSound.GetSound(), Filter.Pvs(args.Target), args.Target);
            _uiSystem.GetUiOrNull(args.Target, WiresUiKey.Key)?.CloseAll();
        }
    }

    private void OnExamine(EntityUid uid, WiresComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString(component.IsPanelOpen
            ? "wires-component-on-examine-panel-open"
            : "wires-component-on-examine-panel-closed"));
    }

    private void OnMapInit(EntityUid uid, WiresComponent component, MapInitEvent args)
    {
        if (component.SerialNumber == null)
        {
            GenerateSerialNumber(uid, component);
        }

        if (component.WireSeed == 0)
        {
            component.WireSeed = _random.Next(1, int.MaxValue);
            UpdateUserInterface(uid);
        }
    }
    #endregion

    #region Entity API
    private void GenerateSerialNumber(EntityUid uid, WiresComponent? wires = null)
    {
        if (!Resolve(uid, ref wires))
            return;

        Span<char> data = stackalloc char[9];
        data[4] = '-';

        if (_random.Prob(0.01f))
        {
            for (var i = 0; i < 4; i++)
            {
                // Cyrillic Letters
                data[i] = (char) _random.Next(0x0410, 0x0430);
            }
        }
        else
        {
            for (var i = 0; i < 4; i++)
            {
                // Letters
                data[i] = (char) _random.Next(0x41, 0x5B);
            }
        }

        for (var i = 5; i < 9; i++)
        {
            // Digits
            data[i] = (char) _random.Next(0x30, 0x3A);
        }

        wires.SerialNumber = new string(data);
        UpdateUserInterface(uid);
    }

    private void UpdateAppearance(EntityUid uid, AppearanceComponent? appearance = null, WiresComponent? wires = null)
    {
        if (!Resolve(uid, ref appearance, ref wires, false))
            return;

        appearance.SetData(WiresVisuals.MaintenancePanelState, wires.IsPanelOpen && wires.IsPanelVisible);
    }

    private void UpdateUserInterface(EntityUid uid, WiresComponent? wires = null, ServerUserInterfaceComponent? ui = null)
    {
        if (!Resolve(uid, ref wires, ref ui, false)) // logging this means that we get a bunch of errors
            return;

        var clientList = new List<ClientWire>();
        foreach (var entry in wires.WiresList)
        {
            clientList.Add(new ClientWire(entry.Id, entry.IsCut, entry.Color,
                entry.Letter));

            var statusData = entry.Action.GetStatusLightData(entry);
            if (statusData != null && entry.Action.StatusKey != null)
            {
                wires.Statuses[entry.Action.StatusKey] = statusData;
            }
        }

        _uiSystem.GetUiOrNull(uid, WiresUiKey.Key)?.SetState(
            new WiresBoundUserInterfaceState(
                clientList.ToArray(),
                wires.Statuses.Select(p => new StatusEntry(p.Key, p.Value)).ToArray(),
                wires.BoardName,
                wires.SerialNumber,
                wires.WireSeed));
    }

    public void OpenUserInterface(EntityUid uid, IPlayerSession player)
    {
        _uiSystem.GetUiOrNull(uid, WiresUiKey.Key)?.Open(player);
    }

    /// <summary>
    ///     Tries to get a wire on this entity by its integer id.
    /// </summary>
    /// <returns>The wire if found, otherwise null</returns>
    public Wire? TryGetWire(EntityUid uid, int id, WiresComponent? wires = null)
    {
        if (!Resolve(uid, ref wires))
            return null;

        return id >= 0 && id < wires.WiresList.Count
            ? wires.WiresList[id]
            : null;
    }

    /// <summary>
    ///     Tries to get all the wires on this entity by the wire action type.
    /// </summary>
    /// <returns>Enumerator of all wires in this entity according to the given type.</returns>
    public IEnumerable<Wire> TryGetWires<T>(EntityUid uid, WiresComponent? wires = null)
    {
        if (!Resolve(uid, ref wires))
            yield break;

        foreach (var wire in wires.WiresList)
        {
            if (wire.GetType() == typeof(T))
            {
                yield return wire;
            }
        }
    }

    private void TryDoWireAction(EntityUid used, EntityUid user, EntityUid toolEntity, int id, WiresAction action, WiresComponent? wires = null, ToolComponent? tool = null)
    {
        if (!Resolve(used, ref wires)
            || !Resolve(toolEntity, ref tool))
            return;

        if (wires.WiresQueue.Contains(id))
            return;

        var wire = TryGetWire(used, id, wires);

        if (wire == null)
            return;

        switch (action)
        {
            case WiresAction.Cut:
                if (!_toolSystem.HasQuality(toolEntity, "Cutting", tool))
                {
                    _popupSystem.PopupCursor(Loc.GetString("wires-component-ui-on-receive-message-need-wirecutters"), Filter.Entities(user));
                    return;
                }

                break;
            case WiresAction.Mend:
                if (!_toolSystem.HasQuality(toolEntity, "Cutting", tool))
                {
                    _popupSystem.PopupCursor(Loc.GetString("wires-component-ui-on-receive-message-need-wirecutters"), Filter.Entities(user));
                    return;
                }

                break;
            case WiresAction.Pulse:
                if (!_toolSystem.HasQuality(toolEntity, "Pulsing", tool))
                {
                    _popupSystem.PopupCursor(Loc.GetString("wires-component-ui-on-receive-message-need-multitool"), Filter.Entities(user));
                    return;
                }

                break;
        }

        if (_toolTime > 0f)
        {
            var args = new DoAfterEventArgs(user, _toolTime, default, used)
            {
                NeedHand = true,
                BreakOnStun = true,
                BreakOnDamage = true,
                BreakOnUserMove = true,
                TargetFinishedEvent = new OnWireDoAfterEvent
                {
                    Target = used,
                    User = user,
                    Tool = toolEntity,
                    Action = action,
                    Id = id
                },
                TargetCancelledEvent = new OnWireDoAfterCancelEvent
                {
                    Id = id
                }
            };

            _doAfter.DoAfter(args);

            wires.WiresQueue.Add(id);
        }
        else
        {
            UpdateWires(used, user, toolEntity, id, action, wires);
        }
    }



    private void UpdateWires(EntityUid used, EntityUid user, EntityUid toolEntity, int id, WiresAction action, WiresComponent? wires = null, ToolComponent? tool = null)
    {
        if (!Resolve(used, ref wires))
            return;

        if (!Resolve(toolEntity, ref tool))
        {
            wires.WiresQueue.Remove(id);
            return;
        }

        var wire = TryGetWire(used, id, wires);

        if (wire == null)
        {
            wires.WiresQueue.Remove(id);
            return;
        }

        switch (action)
        {
            case WiresAction.Cut:
                if (!_toolSystem.HasQuality(toolEntity, "Cutting", tool))
                {
                    _popupSystem.PopupCursor(Loc.GetString("wires-component-ui-on-receive-message-need-wirecutters"), Filter.Entities(user));
                    break;
                }

                _toolSystem.PlayToolSound(toolEntity, tool);
                if (wire.Action.Cut(user, wire))
                {
                    wire.IsCut = true;
                }

                UpdateUserInterface(used);
                break;
            case WiresAction.Mend:
                if (!_toolSystem.HasQuality(toolEntity, "Cutting", tool))
                {
                    _popupSystem.PopupCursor(Loc.GetString("wires-component-ui-on-receive-message-need-wirecutters"), Filter.Entities(user));
                    break;
                }

                _toolSystem.PlayToolSound(toolEntity, tool);
                if (wire.Action.Mend(user, wire))
                {
                    wire.IsCut = false;
                }

                UpdateUserInterface(used);
                break;
            case WiresAction.Pulse:
                if (!_toolSystem.HasQuality(toolEntity, "Pulsing", tool))
                {
                    _popupSystem.PopupCursor(Loc.GetString("wires-component-ui-on-receive-message-need-multitool"), Filter.Entities(user));
                    break;
                }

                if (wire.IsCut)
                {
                    _popupSystem.PopupCursor(Loc.GetString("wires-component-ui-on-receive-message-cannot-pulse-cut-wire"), Filter.Entities(user));
                    break;
                }

                wire.Action.Pulse(user, wire);

                UpdateUserInterface(used);
                SoundSystem.Play(wires.PulseSound.GetSound(), Filter.Pvs(used), used);
                break;
        }

        wires.WiresQueue.Remove(id);
    }

    /// <summary>
    ///     Tries to get the stateful data stored in this entity's WiresComponent.
    /// </summary>
    /// <param id="identifier">The key that stores the data in the WiresComponent.</param>
    public bool TryGetData<T>(EntityUid uid, object identifier, [NotNullWhen(true)] out T? data, WiresComponent? wires = null)
    {
        data = default(T);
        if (!Resolve(uid, ref wires))
            return false;

        wires.StateData.TryGetValue(identifier, out var result);

        if (result is not T)
        {
            return false;
        }

        data = (T) result;

        return true;
    }

    /// <summary>
    ///     Sets data in the entity's WiresComponent state dictionary by key.
    /// </summary>
    /// <param id="identifier">The key that stores the data in the WiresComponent.</param>
    /// <param id="data">The data to store using the given identifier.</param>
    public void SetData(EntityUid uid, object identifier, object data, WiresComponent? wires = null)
    {
        if (!Resolve(uid, ref wires))
            return;

        if (wires.StateData.TryGetValue(identifier, out var storedMessage))
        {
            if (storedMessage == data)
            {
                return;
            }
        }

        wires.StateData[identifier] = data;
        UpdateUserInterface(uid, wires);
    }

    /// <summary>
    ///     If this entity has data stored via this key in the WiresComponent it has
    /// </summary>
    public bool HasData(EntityUid uid, object identifier, WiresComponent? wires = null)
    {
        if (!Resolve(uid, ref wires))
            return false;

        return wires.StateData.ContainsKey(identifier);
    }

    /// <summary>
    ///     Removes data from this entity stored in the given key from the entity's WiresComponent.
    /// </summary>
    /// <param id="identifier">The key that stores the data in the WiresComponent.</param>
    public void RemoveData(EntityUid uid, object identifier, WiresComponent? wires = null)
    {
        if (!Resolve(uid, ref wires))
            return;

        wires.StateData.Remove(identifier);
    }
    #endregion

    #region Layout Handling
    private bool TryGetLayout(string id, [NotNullWhen(true)] out WireLayout? layout)
    {
        return _layouts.TryGetValue(id, out layout);
    }

    private void AddLayout(string id, WireLayout layout)
    {
        _layouts.Add(id, layout);
    }

    private void Reset(RoundRestartCleanupEvent args)
    {
        _layouts.Clear();
        _random = new RobustRandom();
    }
    #endregion

    #region Events
    private sealed class WireToolFinishedEvent : EntityEventArgs
    {
        public EntityUid Target { get; }

        public WireToolFinishedEvent(EntityUid target)
        {
            Target = target;
        }
    }

    private sealed class OnWireDoAfterEvent : EntityEventArgs
    {
        public EntityUid User { get; set; }
        public EntityUid Target { get; set; }
        public EntityUid Tool { get; set; }
        public WiresAction Action { get; set; }
        public int Id { get; set; }
    }

    private sealed class OnWireDoAfterCancelEvent : EntityEventArgs
    {
        public int Id { get; set; }
    }
    #endregion
}

public sealed class Wire
{
    /// <summary>
    /// The entity that registered the wire.
    /// </summary>
    public EntityUid Owner { get; }

    /// <summary>
    /// Whether the wire is cut.
    /// </summary>
    public bool IsCut { get; set; }

    /// <summary>
    /// Used in client-server communication to identify a wire without telling the client what the wire does.
    /// </summary>
    [ViewVariables]
    public int Id { get; set; }

    /// <summary>
    /// The color of the wire.
    /// </summary>
    [ViewVariables]
    public WireColor Color { get; }

    /// <summary>
    /// The greek letter shown below the wire.
    /// </summary>
    [ViewVariables]
    public WireLetter Letter { get; }

    // The action that this wire performs upon activation.
    public IWireAction Action { get; set; }

    public Wire(EntityUid owner, bool isCut, WireColor color, WireLetter letter, IWireAction action)
    {
        Owner = owner;
        IsCut = isCut;
        Color = color;
        Letter = letter;
        Action = action;
    }
}

// this is here so that when a DoAfter event is called,
// WiresSystem can call the action in question after the
// doafter is finished (either through cancellation
// or completion - this is implementation dependent)
public delegate void WireActionDelegate(Wire wire);

// callbacks over the event bus,
// because async is banned
public sealed class TimedWireEvent : EntityEventArgs
{
    /// <summary>
    ///     The function to be called once
    ///     the timed event is complete.
    /// </summary>
    public WireActionDelegate Delegate { get; }

    /// <summary>
    ///     The wire tied to this timed wire event.
    /// </summary>
    public Wire Wire { get; }

    public TimedWireEvent(WireActionDelegate @delegate, Wire wire)
    {
        Delegate = @delegate;
        Wire = wire;
    }
}

public sealed class WireLayout
{
    // why is this an <int, WireData>?
    // List<T>.Insert panics,
    // and I needed a uniquer key for wires
    // which allows me to have a unified identifier
    [ViewVariables] public IReadOnlyDictionary<int, WireData> Specifications { get; }

    public WireLayout(IReadOnlyDictionary<int, WireData> specifications)
    {
        Specifications = specifications;
    }

    public sealed class WireData
    {
        public WireLetter Letter { get; }
        public WireColor Color { get; }
        public int Position { get; }

        public WireData(WireLetter letter, WireColor color, int position)
        {
            Letter = letter;
            Color = color;
            Position = position;
        }
    }
}
