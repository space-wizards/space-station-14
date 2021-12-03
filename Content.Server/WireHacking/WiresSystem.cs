using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Hands.Components;
using Content.Server.Tools;
using Content.Server.Tools.Components;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Sound;
using Content.Shared.Wires;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Reflection;
using Robust.Shared.ViewVariables;

namespace Content.Server.Wires;

public class WiresSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AudioSystem _audioSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly ToolSystem _toolSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;

    // This is where all the wire layouts are stored.
    [ViewVariables] private readonly Dictionary<string, WireLayout> _layouts = new();

    public const float ScrewTime = 2.5f;

    #region Initialization
    public override void Initialize()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);

        // this is a broadcast event
        SubscribeLocalEvent<WireToolFinishedEvent>(OnToolFinished);
        SubscribeLocalEvent<WiresComponent, ComponentStartup>(OnWiresStartup);
        SubscribeLocalEvent<WiresComponent, WiresActionMessage>(OnWiresActionMessage);
        SubscribeLocalEvent<WiresComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<WiresComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<WiresComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<WiresComponent, WireDoAfterEvent>(OnWireDoAfter);
    }

    private void SetOrCreateWireLayout(EntityUid uid, WiresComponent? wires = null)
    {
        if (!Resolve(uid, ref wires))
            return;

        WireLayout? layout = null;
        if (wires.LayoutId != null)
            TryGetLayout(wires.LayoutId, out layout);

        var wireSet = CreateWireSet(uid, layout, wires);

        if (wireSet != null)
            wires.WiresList.AddRange(wireSet);

        if (layout != null)
        {
            wires.WiresList.Sort((a, b) =>
            {
                var pA = layout.Specifications[a.Identifier].Position;
                var pB = layout.Specifications[b.Identifier].Position;

                return pA.CompareTo(pB);
            });
        }
        else
        {
            _random.Shuffle(wires.WiresList);

            if (wires.LayoutId != null)
            {
                var dict = new Dictionary<object, WireLayout.WireData>();
                for (var i = 0; i < wires.WiresList.Count; i++)
                {
                    var d = wires.WiresList[i];
                    dict.Add(d.Identifier, new WireLayout.WireData(d.Letter, d.Color, i));
                }

                AddLayout(wires.LayoutId, new WireLayout(dict));
            }
        }

        var id = 0;
        foreach (var wire in wires.WiresList)
        {
            wire.Id = ++id;
            wire.Action.Initialize(uid, wire);
        }
    }

    private List<Wire>? CreateWireSet(EntityUid uid, WireLayout? layout, WiresComponent? wires = null)
    {
        if (!Resolve(uid, ref wires))
            return null;

        List<WireColor> colors =
            new((WireColor[]) Enum.GetValues(typeof(WireColor)));

        List<WireLetter> letters =
            new((WireLetter[]) Enum.GetValues(typeof(WireLetter)));

        var wireSet = new List<Wire>();
        foreach (var wire in wires.WireActions)
        {
            wireSet.Add(CreateWire(uid, wire, layout, colors, letters));
        }

        return wireSet;
    }

    private Wire CreateWire(EntityUid uid, IWireAction action, WireLayout? layout, List<WireColor> colors, List<WireLetter> letters)
    {
        WireLetter letter;
        WireColor color;

        if (layout != null
            && layout.Specifications.TryGetValue(action.Identifier, out var spec))
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
            action.Identifier,
            action);
    }

    private void OnWiresStartup(EntityUid uid, WiresComponent component, ComponentStartup args)
    {
        SetOrCreateWireLayout(uid, component);
        UpdateUserInterface(uid);
    }
    #endregion

    #region Event Handling
    private void OnWireDoAfter(EntityUid uid, WiresComponent component, WireDoAfterEvent args)
    {
        args.Delegate(uid, args.Wire);
    }

    private void OnWiresActionMessage(EntityUid uid, WiresComponent component, WiresActionMessage args)
    {
        // var wire = component.WiresList.Find(x => x.Id == args.Id);
        var player = args.Session.AttachedEntity;
        if (player == null)
        {
            return;
        }

        if (!EntityManager.TryGetComponent(player.Uid, out HandsComponent? handsComponent))
        {
            _popupSystem.PopupEntity(Loc.GetString("wires-component-ui-on-receive-message-no-hands"), uid, Filter.Entities(player.Uid));
            return;
        }

        // it would be odd if the entity was removed in the middle of this synchronous
        // operation, wouldn't it?
        if (!_interactionSystem.InRangeUnobstructed(player, EntityManager.GetEntity(uid)))
        {
            _popupSystem.PopupEntity(Loc.GetString("wires-component-ui-on-receive-message-cannot-reach"), uid, Filter.Entities(player.Uid));
            return;
        }

        var activeHand = handsComponent.GetActiveHand;
        if (activeHand == null)
            return;

        var activeHandEntity = activeHand.OwnerUid;
        if (!EntityManager.TryGetComponent(activeHandEntity, out ToolComponent? tool))
            return;

        UpdateWires(uid, player.Uid, activeHandEntity, args.Id, args.Action, component, tool);
    }

    private void OnInteractUsing(EntityUid uid, WiresComponent component, InteractUsingEvent args)
    {
        Logger.DebugS("Wires", "Attempting to interact using something");
        if (!EntityManager.TryGetComponent(args.UsedUid, out ToolComponent? tool))
            return;

        if (component.IsPanelOpen &&
            _toolSystem.HasQuality(args.UsedUid, "Cutting", tool) ||
            _toolSystem.HasQuality(args.UsedUid, "Pulsing", tool))
        {
            if (EntityManager.TryGetComponent(args.UserUid, out ActorComponent? actor))
            {
                _uiSystem.GetUiOrNull(uid, WiresUiKey.Key)?.Open(actor.PlayerSession);
                args.Handled = true;
            }
        }
        else if (_toolSystem.UseTool(args.UsedUid, args.UserUid, uid, 0f, ScrewTime, new string[]{ "Screwing" }, doAfterCompleteEvent:new WireToolFinishedEvent(uid), toolComponent:tool))
        {
            Logger.DebugS("Wires", "Trying to unscrew now...");
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
            SoundSystem.Play(Filter.Pvs(args.Target), component.ScrewdriverOpenSound.GetSound(), args.Target);
        }
        else
        {
            SoundSystem.Play(Filter.Pvs(args.Target), component.ScrewdriverCloseSound.GetSound(), args.Target);
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
        if (!Resolve(uid, ref appearance, ref wires))
            return;

        appearance.SetData(WiresVisuals.MaintenancePanelState, wires.IsPanelOpen && wires.IsPanelVisible);
    }

    private void UpdateUserInterface(EntityUid uid, WiresComponent? wires = null)
    {
        if (!Resolve(uid, ref wires))
            return;

        var clientList = new List<ClientWire>();
        foreach (var entry in wires.WiresList)
        {
            clientList.Add(new ClientWire(entry.Id, entry.IsCut, entry.Color,
                entry.Letter));
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

    public Wire? TryGetWire(EntityUid uid, int id, WiresComponent? wires = null)
    {
        if (!Resolve(uid, ref wires))
            return null;

        return wires.WiresList.Find(x => x.Id == id);
    }

    public Wire? TryGetWire(EntityUid uid, object id, WiresComponent? wires = null)
    {
        if (!Resolve(uid, ref wires))
            return null;

        return wires.WiresList.Find(x => x.Identifier == id);
    }

    public void UpdateWires(EntityUid used, EntityUid user, EntityUid toolEntity, int id, WiresAction action, WiresComponent? wires = null, ToolComponent? tool = null)
    {
        if (!Resolve(used, ref wires)
            || !Resolve(toolEntity, ref tool))
            return;

        var wire = wires.WiresList.Find(x => x.Id == id);

        if (wire == null)
            return;

        // This is the big part of this.
        // How do you get wire actions?
        // Where should wires be stored?
        // Where should wire state be stored?
        //
        switch (action)
        {
            case WiresAction.Cut:
                if (!_toolSystem.HasQuality(toolEntity, "Cutting", tool))
                {
                    _popupSystem.PopupCursor(Loc.GetString("wires-component-ui-on-receive-message-need-wirecutters"), Filter.Entities(user));
                    return;
                }

                _toolSystem.PlayToolSound(toolEntity, tool);
                wire.Action.Cut(used, user, wire);
                UpdateUserInterface(used);
                break;
            case WiresAction.Mend:
                if (!_toolSystem.HasQuality(toolEntity, "Cutting", tool))
                {
                    _popupSystem.PopupCursor(Loc.GetString("wires-component-ui-on-receive-message-need-wirecutters"), Filter.Entities(user));
                    return;
                }

                _toolSystem.PlayToolSound(toolEntity, tool);
                wire.Action.Mend(used, user, wire);
                UpdateUserInterface(used);
                break;
            case WiresAction.Pulse:
                if (!_toolSystem.HasQuality(toolEntity, "Pulsing", tool))
                {
                    _popupSystem.PopupCursor(Loc.GetString("wires-component-ui-on-receive-message-need-wirecutters"), Filter.Entities(user));
                    return;
                }

                if (wire.IsCut)
                {
                    _popupSystem.PopupCursor(Loc.GetString("wires-component-ui-on-receive-message-cannot-pulse-cut-wire"), Filter.Entities(user));
                    return;
                }

                wire.Action.Pulse(used, user, wire);
                SoundSystem.Play(Filter.Pvs(used), wires.PulseSound.GetSound(), used);
                break;
        }
    }

    public bool TryGetData(EntityUid uid, object identifier, [NotNullWhen(true)] out object? data, WiresComponent? wires = null)
    {
        data = null;
        if (!Resolve(uid, ref wires))
            return false;

        return wires.StateData.TryGetValue(identifier, out data);
    }

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
    }


    // This should be used by IWireActions to set the current
    // visual state of the wire.
    public void SetStatus(EntityUid uid, object statusIdentifier, object status, WiresComponent? wires = null)
    {
        if (!Resolve(uid, ref wires))
            return;

        if (wires.Statuses.TryGetValue(statusIdentifier, out var storedMessage))
        {
            if (storedMessage == status)
            {
                return;
            }
        }

        wires.Statuses[statusIdentifier] = status;
        UpdateUserInterface(uid);
    }
    #endregion

    #region Layout Handling
    public bool TryGetLayout(string id, [NotNullWhen(true)] out WireLayout? layout)
    {
        return _layouts.TryGetValue(id, out layout);
    }

    public void AddLayout(string id, WireLayout layout)
    {
        _layouts.Add(id, layout);
    }

    public void Reset(RoundRestartCleanupEvent args)
    {
        _layouts.Clear();
    }
    #endregion

    #region Events
    private class WireToolFinishedEvent : EntityEventArgs
    {
        public EntityUid Target { get; }

        public WireToolFinishedEvent(EntityUid target)
        {
            Target = target;
        }
    }
    #endregion

}

public class Wire
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

    /// <summary>
    /// Registered by components implementing IWires, used to identify which wire the client interacted with.
    /// </summary>
    [ViewVariables]
    public object Identifier { get; }

    // The action that this wire performs upon activation.
    public IWireAction Action { get; }

    public Wire(EntityUid owner, bool isCut, WireColor color, WireLetter letter, object identifier, IWireAction action)
    {
        Owner = owner;
        IsCut = isCut;
        Color = color;
        Letter = letter;
        Identifier = identifier;
        Action = action;
    }
}

// this is here so that when a DoAfter event is called,
// WiresSystem can call the action in question after the
// doafter is finished (either through cancellation
// or completion - this is implementation dependent)
public delegate void WireActionDelegate(EntityUid used, Wire wire);

// callbacks over the event bus,
// because async is banned
public class WireDoAfterEvent : EntityEventArgs
{
    public WireActionDelegate Delegate { get; }
    public Wire Wire { get; }

    public WireDoAfterEvent(WireActionDelegate @delegate, Wire wire)
    {
        Delegate = @delegate;
        Wire = wire;
    }
}

public sealed class WireLayout
{
    [ViewVariables] public IReadOnlyDictionary<object, WireData> Specifications { get; }

    public WireLayout(IReadOnlyDictionary<object, WireData> specifications)
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
