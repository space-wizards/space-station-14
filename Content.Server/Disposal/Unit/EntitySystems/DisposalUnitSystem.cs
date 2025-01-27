using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Disposal.Tube;
using Content.Server.Disposal.Tube.Components;
using Content.Server.Disposal.Unit.Components;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.ActionBlocker;
using Content.Shared.Atmos;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.Disposal;
using Content.Shared.Disposal.Components;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Emag.Systems;
using Content.Shared.Explosion;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Verbs;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Disposal.Unit.EntitySystems;

public sealed class DisposalUnitSystem : SharedDisposalUnitSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly AtmosphereSystem _atmosSystem = default!;
    [Dependency] private readonly AudioSystem _audioSystem = default!;
    [Dependency] private readonly DisposalTubeSystem _disposalTubeSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DisposalUnitComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<DisposalUnitComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<DisposalUnitComponent, CanDropTargetEvent>(OnCanDragDropOn);
        SubscribeLocalEvent<DisposalUnitComponent, GotEmaggedEvent>(OnEmagged);

        // Shouldn't need re-anchoring.
        SubscribeLocalEvent<DisposalUnitComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        // TODO: Predict me when hands predicted
        SubscribeLocalEvent<DisposalUnitComponent, ContainerRelayMovementEntityEvent>(OnMovement);
        SubscribeLocalEvent<DisposalUnitComponent, PowerChangedEvent>(OnPowerChange);
        SubscribeLocalEvent<DisposalUnitComponent, ComponentInit>(OnDisposalInit);

        SubscribeLocalEvent<DisposalUnitComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<DisposalUnitComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
        SubscribeLocalEvent<DisposalUnitComponent, DragDropTargetEvent>(OnDragDropOn);
        SubscribeLocalEvent<DisposalUnitComponent, DestructionEventArgs>(OnDestruction);
        SubscribeLocalEvent<DisposalUnitComponent, BeforeExplodeEvent>(OnExploded);

        SubscribeLocalEvent<DisposalUnitComponent, GetVerbsEvent<InteractionVerb>>(AddInsertVerb);
        SubscribeLocalEvent<DisposalUnitComponent, GetVerbsEvent<AlternativeVerb>>(AddDisposalAltVerbs);
        SubscribeLocalEvent<DisposalUnitComponent, GetVerbsEvent<Verb>>(AddClimbInsideVerb);

        SubscribeLocalEvent<DisposalUnitComponent, DisposalDoAfterEvent>(OnDoAfter);

        SubscribeLocalEvent<DisposalUnitComponent, SharedDisposalUnitComponent.UiButtonPressedMessage>(OnUiButtonPressed);
    }

    private void OnGetState(EntityUid uid, DisposalUnitComponent component, ref ComponentGetState args)
    {
        args.State = new DisposalUnitComponentState(
            component.FlushSound,
            component.State,
            component.NextPressurized,
            component.AutomaticEngageTime,
            component.NextFlush,
            component.Powered,
            component.Engaged,
            GetNetEntityList(component.RecentlyEjected));
    }

    private void AddDisposalAltVerbs(EntityUid uid, SharedDisposalUnitComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        // Behavior for if the disposals bin has items in it
        if (component.Container.ContainedEntities.Count > 0)
        {
            // Verbs to flush the unit
            AlternativeVerb flushVerb = new()
            {
                Act = () => ManualEngage(uid, component),
                Text = Loc.GetString("disposal-flush-verb-get-data-text"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/delete_transparent.svg.192dpi.png")),
                Priority = 1,
            };
            args.Verbs.Add(flushVerb);

            // Verb to eject the contents
            AlternativeVerb ejectVerb = new()
            {
                Act = () => TryEjectContents(uid, component),
                Category = VerbCategory.Eject,
                Text = Loc.GetString("disposal-eject-verb-get-data-text")
            };
            args.Verbs.Add(ejectVerb);
        }
    }

    private void AddClimbInsideVerb(EntityUid uid, SharedDisposalUnitComponent component, GetVerbsEvent<Verb> args)
    {
        // This is not an interaction, activation, or alternative verb type because unfortunately most users are
        // unwilling to accept that this is where they belong and don't want to accidentally climb inside.
        if (!args.CanAccess ||
            !args.CanInteract ||
            component.Container.ContainedEntities.Contains(args.User) ||
            !_actionBlockerSystem.CanMove(args.User))
        {
            return;
        }

        if (!CanInsert(uid, component, args.User))
            return;

        // Add verb to climb inside of the unit,
        Verb verb = new()
        {
            Act = () => TryInsert(uid, args.User, args.User),
            DoContactInteraction = true,
            Text = Loc.GetString("disposal-self-insert-verb-get-data-text")
        };
        // TODO VERB ICON
        // TODO VERB CATEGORY
        // create a verb category for "enter"?
        // See also, medical scanner. Also maybe add verbs for entering lockers/body bags?
        args.Verbs.Add(verb);
    }

    private void AddInsertVerb(EntityUid uid, SharedDisposalUnitComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null || args.Using == null)
            return;

        if (!_actionBlockerSystem.CanDrop(args.User))
            return;

        if (!CanInsert(uid, component, args.Using.Value))
            return;

        InteractionVerb insertVerb = new()
        {
            Text = Name(args.Using.Value),
            Category = VerbCategory.Insert,
            Act = () =>
            {
                _handsSystem.TryDropIntoContainer(args.User, args.Using.Value, component.Container, checkActionBlocker: false, args.Hands);
                _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(args.User):player} inserted {ToPrettyString(args.Using.Value)} into {ToPrettyString(uid)}");
                AfterInsert(uid, component, args.Using.Value, args.User);
            }
        };

        args.Verbs.Add(insertVerb);
    }

    private void OnDoAfter(EntityUid uid, SharedDisposalUnitComponent component, DoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null || args.Args.Used == null)
            return;

        AfterInsert(uid, component, args.Args.Target.Value, args.Args.User, doInsert: true);

        args.Handled = true;
    }

    public override void DoInsertDisposalUnit(EntityUid uid, EntityUid toInsert, EntityUid user, SharedDisposalUnitComponent? disposal = null)
    {
        if (!ResolveDisposals(uid, ref disposal))
            return;

        if (!_containerSystem.Insert(toInsert, disposal.Container))
            return;

        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(user):player} inserted {ToPrettyString(toInsert)} into {ToPrettyString(uid)}");
        AfterInsert(uid, disposal, toInsert, user);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = AllEntityQuery<DisposalUnitComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var unit, out var metadata))
        {
            if (!metadata.EntityPaused)
                Update(uid, unit, metadata, frameTime);
        }
    }

    #region UI Handlers
    private void OnUiButtonPressed(EntityUid uid, SharedDisposalUnitComponent component, SharedDisposalUnitComponent.UiButtonPressedMessage args)
    {
        if (args.Actor is not { Valid: true } player)
        {
            return;
        }

        switch (args.Button)
        {
            case SharedDisposalUnitComponent.UiButton.Eject:
                TryEjectContents(uid, component);
                _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(player):player} hit eject button on {ToPrettyString(uid)}");
                break;
            case SharedDisposalUnitComponent.UiButton.Engage:
                ToggleEngage(uid, component);
                _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(player):player} hit flush button on {ToPrettyString(uid)}, it's now {(component.Engaged ? "on" : "off")}");
                break;
            case SharedDisposalUnitComponent.UiButton.Power:
                _power.TogglePower(uid, user: args.Actor);
                break;
            default:
                throw new ArgumentOutOfRangeException($"{ToPrettyString(player):player} attempted to hit a nonexistant button on {ToPrettyString(uid)}");
        }
    }

    public void ToggleEngage(EntityUid uid, SharedDisposalUnitComponent component)
    {
        component.Engaged ^= true;

        if (component.Engaged)
        {
            ManualEngage(uid, component);
        }
        else
        {
            Disengage(uid, component);
        }
    }

    #endregion

    #region Eventbus Handlers

    private void OnActivate(EntityUid uid, SharedDisposalUnitComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        if (!TryComp(args.User, out ActorComponent? actor))
        {
            return;
        }

        args.Handled = true;
        _ui.OpenUi(uid, SharedDisposalUnitComponent.DisposalUnitUiKey.Key, actor.PlayerSession);
    }

    private void OnAfterInteractUsing(EntityUid uid, SharedDisposalUnitComponent component, AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (!HasComp<HandsComponent>(args.User))
        {
            return;
        }

        if (!CanInsert(uid, component, args.Used) || !_handsSystem.TryDropIntoContainer(args.User, args.Used, component.Container))
        {
            return;
        }

        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(args.User):player} inserted {ToPrettyString(args.Used)} into {ToPrettyString(uid)}");
        AfterInsert(uid, component, args.Used, args.User);
        args.Handled = true;
    }

    private void OnDisposalInit(EntityUid uid, SharedDisposalUnitComponent component, ComponentInit args)
    {
        component.Container = _containerSystem.EnsureContainer<Container>(uid, SharedDisposalUnitComponent.ContainerId);

        UpdateInterface(uid, component, component.Powered);
    }

    private void OnPowerChange(EntityUid uid, SharedDisposalUnitComponent component, ref PowerChangedEvent args)
    {
        if (!component.Running || args.Powered == component.Powered)
            return;

        component.Powered = args.Powered;
        UpdateVisualState(uid, component);
        UpdateInterface(uid, component, args.Powered);

        if (!args.Powered)
        {
            component.NextFlush = null;
            Dirty(uid, component);
            return;
        }

        if (component.Engaged)
        {
            // Run ManualEngage to recalculate a new flush time
            ManualEngage(uid, component);
        }
    }

    // TODO: This should just use the same thing as entity storage?
    private void OnMovement(EntityUid uid, SharedDisposalUnitComponent component, ref ContainerRelayMovementEntityEvent args)
    {
        var currentTime = GameTiming.CurTime;

        if (!_actionBlockerSystem.CanMove(args.Entity))
            return;

        if (!TryComp(args.Entity, out HandsComponent? hands) ||
            hands.Count == 0 ||
            currentTime < component.LastExitAttempt + ExitAttemptDelay)
            return;

        component.LastExitAttempt = currentTime;
        Remove(uid, component, args.Entity);
    }

    private void OnAnchorChanged(EntityUid uid, SharedDisposalUnitComponent component, ref AnchorStateChangedEvent args)
    {
        if (Terminating(uid))
            return;

        UpdateVisualState(uid, component);
        if (!args.Anchored)
            TryEjectContents(uid, component);
    }

    private void OnDestruction(EntityUid uid, SharedDisposalUnitComponent component, DestructionEventArgs args)
    {
        TryEjectContents(uid, component);
    }

    private void OnDragDropOn(EntityUid uid, SharedDisposalUnitComponent component, ref DragDropTargetEvent args)
    {
        args.Handled = TryInsert(uid, args.Dragged, args.User);
    }

    #endregion

    private void UpdateState(EntityUid uid, DisposalsPressureState state, SharedDisposalUnitComponent component, MetaDataComponent metadata)
    {
        if (component.State == state)
            return;

        component.State = state;
        UpdateVisualState(uid, component);
        UpdateInterface(uid, component, component.Powered);
        Dirty(uid, component, metadata);

        if (state == DisposalsPressureState.Ready)
        {
            component.NextPressurized = TimeSpan.Zero;

            // Manually engaged
            if (component.Engaged)
            {
                component.NextFlush = GameTiming.CurTime + component.ManualFlushTime;
            }
            else if (component.Container.ContainedEntities.Count > 0)
            {
                component.NextFlush = GameTiming.CurTime + component.AutomaticEngageTime;
            }
            else
            {
                component.NextFlush = null;
            }
        }
    }

    /// <summary>
    /// Work out if we can stop updating this disposals component i.e. full pressure and nothing colliding.
    /// </summary>
    private void Update(EntityUid uid, SharedDisposalUnitComponent component, MetaDataComponent metadata, float frameTime)
    {
        var state = GetState(uid, component, metadata);

        // Pressurizing, just check if we need a state update.
        if (component.NextPressurized > GameTiming.CurTime)
        {
            UpdateState(uid, state, component, metadata);
            return;
        }

        if (component.NextFlush != null)
        {
            if (component.NextFlush.Value < GameTiming.CurTime)
            {
                TryFlush(uid, component);
            }
        }

        UpdateState(uid, state, component, metadata);

        Box2? disposalsBounds = null;
        var count = component.RecentlyEjected.Count;

        if (count > 0)
        {
            if (!HasComp<PhysicsComponent>(uid))
            {
                component.RecentlyEjected.Clear();
            }
            else
            {
                disposalsBounds = _lookup.GetWorldAABB(uid);
            }
        }

        for (var i = 0; i < component.RecentlyEjected.Count; i++)
        {
            var ejectedId = component.RecentlyEjected[i];
            if (HasComp<PhysicsComponent>(ejectedId))
            {
                // TODO: We need to use a specific collision method (which sloth hasn't coded yet) for actual bounds overlaps.
                // TODO: Come do this sloth :^)
                // Check for itemcomp as we won't just block the disposal unit "sleeping" for something it can't collide with anyway.
                if (!HasComp<ItemComponent>(ejectedId)
                    && _lookup.GetWorldAABB(ejectedId).Intersects(disposalsBounds!.Value))
                {
                    continue;
                }

                component.RecentlyEjected.RemoveAt(i);
                i--;
            }
        }

        if (count != component.RecentlyEjected.Count)
            Dirty(uid, component, metadata);
    }

    public bool TryInsert(EntityUid unitId, EntityUid toInsertId, EntityUid? userId, DisposalUnitComponent? unit = null)
    {
        if (!Resolve(unitId, ref unit))
            return false;

        if (userId.HasValue && !HasComp<HandsComponent>(userId) && toInsertId != userId) // Mobs like mouse can Jump inside even with no hands
        {
            _popupSystem.PopupEntity(Loc.GetString("disposal-unit-no-hands"), userId.Value, userId.Value, PopupType.SmallCaution);
            return false;
        }

        if (!CanInsert(unitId, unit, toInsertId))
            return false;

        bool insertingSelf = userId == toInsertId;

        var delay = insertingSelf ? unit.EntryDelay : unit.DraggedEntryDelay;

        if (userId != null && !insertingSelf)
            _popupSystem.PopupEntity(Loc.GetString("disposal-unit-being-inserted", ("user", Identity.Entity((EntityUid)userId, EntityManager))), toInsertId, toInsertId, PopupType.Large);

        if (delay <= 0 || userId == null)
        {
            AfterInsert(unitId, unit, toInsertId, userId, doInsert: true);
            return true;
        }

        // Can't check if our target AND disposals moves currently so we'll just check target.
        // if you really want to check if disposals moves then add a predicate.
        var doAfterArgs = new DoAfterArgs(EntityManager, userId.Value, delay, new DisposalDoAfterEvent(), unitId, target: toInsertId, used: unitId)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = false,
        };

        _doAfterSystem.TryStartDoAfter(doAfterArgs);
        return true;
    }


    public bool TryFlush(EntityUid uid, SharedDisposalUnitComponent component)
    {
        if (!CanFlush(uid, component))
        {
            return false;
        }

        if (component.NextFlush != null)
            component.NextFlush = component.NextFlush.Value + component.AutomaticEngageTime;

        var beforeFlushArgs = new BeforeDisposalFlushEvent();
        RaiseLocalEvent(uid, beforeFlushArgs);

        if (beforeFlushArgs.Cancelled)
        {
            Disengage(uid, component);
            return false;
        }

        var xform = Transform(uid);
        if (!TryComp(xform.GridUid, out MapGridComponent? grid))
            return false;

        var coords = xform.Coordinates;
        var entry = _map.GetLocal(xform.GridUid.Value, grid, coords)
            .FirstOrDefault(HasComp<DisposalEntryComponent>);

        if (entry == default || component is not DisposalUnitComponent sDisposals)
        {
            component.Engaged = false;
            Dirty(uid, component);
            return false;
        }

        HandleAir(uid, sDisposals, xform);

        _disposalTubeSystem.TryInsert(entry, sDisposals, beforeFlushArgs.Tags);

        component.NextPressurized = GameTiming.CurTime;
        if (!component.DisablePressure)
            component.NextPressurized += TimeSpan.FromSeconds(1f / PressurePerSecond);

        component.Engaged = false;
        // stop queuing NOW
        component.NextFlush = null;

        UpdateVisualState(uid, component, true);
        UpdateInterface(uid, component, component.Powered);

        Dirty(uid, component);

        return true;
    }

    private void HandleAir(EntityUid uid, DisposalUnitComponent component, TransformComponent xform)
    {
        var air = component.Air;
        var indices = _transformSystem.GetGridTilePositionOrDefault((uid, xform));

        if (_atmosSystem.GetTileMixture(xform.GridUid, xform.MapUid, indices, true) is { Temperature: > 0f } environment)
        {
            var transferMoles = 0.1f * (0.25f * Atmospherics.OneAtmosphere * 1.01f - air.Pressure) * air.Volume / (environment.Temperature * Atmospherics.R);

            component.Air = environment.Remove(transferMoles);
        }
    }

    public void UpdateInterface(EntityUid uid, SharedDisposalUnitComponent component, bool powered)
    {
        var compState = GetState(uid, component);
        var stateString = Loc.GetString($"disposal-unit-state-{compState}");
        var state = new SharedDisposalUnitComponent.DisposalUnitBoundUserInterfaceState(Name(uid), stateString, EstimatedFullPressure(uid, component), powered, component.Engaged);
        _ui.SetUiState(uid, SharedDisposalUnitComponent.DisposalUnitUiKey.Key, state);

        var stateUpdatedEvent = new DisposalUnitUIStateUpdatedEvent(state);
        RaiseLocalEvent(uid, stateUpdatedEvent);
    }

    /// <summary>
    /// Returns the estimated time when the disposal unit will be back to full pressure.
    /// </summary>
    private TimeSpan EstimatedFullPressure(EntityUid uid, SharedDisposalUnitComponent component)
    {
        if (component.NextPressurized < GameTiming.CurTime)
            return TimeSpan.Zero;

        return component.NextPressurized;
    }

    public void UpdateVisualState(EntityUid uid, SharedDisposalUnitComponent component, bool flush = false)
    {
        if (!TryComp(uid, out AppearanceComponent? appearance))
        {
            return;
        }

        if (!Transform(uid).Anchored)
        {
            _appearance.SetData(uid, SharedDisposalUnitComponent.Visuals.VisualState, SharedDisposalUnitComponent.VisualState.UnAnchored, appearance);
            _appearance.SetData(uid, SharedDisposalUnitComponent.Visuals.Handle, SharedDisposalUnitComponent.HandleState.Normal, appearance);
            _appearance.SetData(uid, SharedDisposalUnitComponent.Visuals.Light, SharedDisposalUnitComponent.LightStates.Off, appearance);
            return;
        }

        var state = GetState(uid, component);

        switch (state)
        {
            case DisposalsPressureState.Flushed:
                _appearance.SetData(uid, SharedDisposalUnitComponent.Visuals.VisualState, SharedDisposalUnitComponent.VisualState.OverlayFlushing, appearance);
                break;
            case DisposalsPressureState.Pressurizing:
                _appearance.SetData(uid, SharedDisposalUnitComponent.Visuals.VisualState, SharedDisposalUnitComponent.VisualState.OverlayCharging, appearance);
                break;
            case DisposalsPressureState.Ready:
                _appearance.SetData(uid, SharedDisposalUnitComponent.Visuals.VisualState, SharedDisposalUnitComponent.VisualState.Anchored, appearance);
                break;
        }

        _appearance.SetData(uid, SharedDisposalUnitComponent.Visuals.Handle, component.Engaged
            ? SharedDisposalUnitComponent.HandleState.Engaged
            : SharedDisposalUnitComponent.HandleState.Normal, appearance);

        if (!component.Powered)
        {
            _appearance.SetData(uid, SharedDisposalUnitComponent.Visuals.Light, SharedDisposalUnitComponent.LightStates.Off, appearance);
            return;
        }

        var lightState = SharedDisposalUnitComponent.LightStates.Off;

        if (component.Container.ContainedEntities.Count > 0)
        {
            lightState |= SharedDisposalUnitComponent.LightStates.Full;
        }

        if (state is DisposalsPressureState.Pressurizing or DisposalsPressureState.Flushed)
        {
            lightState |= SharedDisposalUnitComponent.LightStates.Charging;
        }
        else
        {
            lightState |= SharedDisposalUnitComponent.LightStates.Ready;
        }

        _appearance.SetData(uid, SharedDisposalUnitComponent.Visuals.Light, lightState, appearance);
    }

    public void Remove(EntityUid uid, SharedDisposalUnitComponent component, EntityUid toRemove)
    {
        _containerSystem.Remove(toRemove, component.Container);

        if (component.Container.ContainedEntities.Count == 0)
        {
            // If not manually engaged then reset the flushing entirely.
            if (!component.Engaged)
            {
                component.NextFlush = null;
            }
        }

        if (!component.RecentlyEjected.Contains(toRemove))
            component.RecentlyEjected.Add(toRemove);

        UpdateVisualState(uid, component);
        Dirty(uid, component);
    }

    public bool CanFlush(EntityUid unit, SharedDisposalUnitComponent component)
    {
        return GetState(unit, component) == DisposalsPressureState.Ready
               && component.Powered
               && Comp<TransformComponent>(unit).Anchored;
    }

    public void ManualEngage(EntityUid uid, SharedDisposalUnitComponent component, MetaDataComponent? metadata = null)
    {
        component.Engaged = true;
        UpdateVisualState(uid, component);
        UpdateInterface(uid, component, component.Powered);
        Dirty(uid, component);

        if (!CanFlush(uid, component))
            return;

        if (!Resolve(uid, ref metadata))
            return;

        var pauseTime = Metadata.GetPauseTime(uid, metadata);
        var nextEngage = GameTiming.CurTime - pauseTime + component.ManualFlushTime;
        component.NextFlush = TimeSpan.FromSeconds(Math.Min((component.NextFlush ?? TimeSpan.MaxValue).TotalSeconds, nextEngage.TotalSeconds));
    }

    public void Disengage(EntityUid uid, SharedDisposalUnitComponent component)
    {
        component.Engaged = false;

        if (component.Container.ContainedEntities.Count == 0)
        {
            component.NextFlush = null;
        }

        UpdateVisualState(uid, component);
        UpdateInterface(uid, component, component.Powered);
        Dirty(uid, component);
    }

    /// <summary>
    /// Remove all entities currently in the disposal unit.
    /// </summary>
    public void TryEjectContents(EntityUid uid, SharedDisposalUnitComponent component)
    {
        foreach (var entity in component.Container.ContainedEntities.ToArray())
        {
            Remove(uid, component, entity);
        }

        if (!component.Engaged)
        {
            component.NextFlush = null;
            Dirty(uid, component);
        }
    }

    public override bool HasDisposals(EntityUid? uid)
    {
        return HasComp<DisposalUnitComponent>(uid);
    }

    public override bool ResolveDisposals(EntityUid uid, [NotNullWhen(true)] ref SharedDisposalUnitComponent? component)
    {
        if (component != null)
            return true;

        TryComp<DisposalUnitComponent>(uid, out var storage);
        component = storage;
        return component != null;
    }

    public override bool CanInsert(EntityUid uid, SharedDisposalUnitComponent component, EntityUid entity)
    {
        if (!base.CanInsert(uid, component, entity))
            return false;

        return _containerSystem.CanInsert(entity, component.Container);
    }

    /// <summary>
    /// If something is inserted (or the likes) then we'll queue up an automatic flush in the future.
    /// </summary>
    public void QueueAutomaticEngage(EntityUid uid, SharedDisposalUnitComponent component, MetaDataComponent? metadata = null)
    {
        if (component.Deleted || !component.AutomaticEngage || !component.Powered && component.Container.ContainedEntities.Count == 0)
        {
            return;
        }

        var pauseTime = Metadata.GetPauseTime(uid, metadata);
        var automaticTime = GameTiming.CurTime + component.AutomaticEngageTime - pauseTime;
        var flushTime = TimeSpan.FromSeconds(Math.Min((component.NextFlush ?? TimeSpan.MaxValue).TotalSeconds, automaticTime.TotalSeconds));

        component.NextFlush = flushTime;
        Dirty(uid, component);
    }

    public void AfterInsert(EntityUid uid, SharedDisposalUnitComponent component, EntityUid inserted, EntityUid? user = null, bool doInsert = false)
    {
        _audioSystem.PlayPvs(component.InsertSound, uid);

        if (doInsert && !_containerSystem.Insert(inserted, component.Container))
            return;

        if (user != inserted && user != null)
            _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(user.Value):player} inserted {ToPrettyString(inserted)} into {ToPrettyString(uid)}");

        QueueAutomaticEngage(uid, component);

        _ui.CloseUi(uid, SharedDisposalUnitComponent.DisposalUnitUiKey.Key, inserted);

        // Maybe do pullable instead? Eh still fine.
        Joints.RecursiveClearJoints(inserted);
        UpdateVisualState(uid, component);
    }

    private void OnExploded(Entity<DisposalUnitComponent> ent, ref BeforeExplodeEvent args)
    {
        args.Contents.AddRange(ent.Comp.Container.ContainedEntities);
    }

}

/// <summary>
/// Sent before the disposal unit flushes it's contents.
/// Allows adding tags for sorting and preventing the disposal unit from flushing.
/// </summary>
public sealed class DisposalUnitUIStateUpdatedEvent : EntityEventArgs
{
    public SharedDisposalUnitComponent.DisposalUnitBoundUserInterfaceState State;

    public DisposalUnitUIStateUpdatedEvent(SharedDisposalUnitComponent.DisposalUnitBoundUserInterfaceState state)
    {
        State = state;
    }
}

/// <summary>
/// Sent before the disposal unit flushes it's contents.
/// Allows adding tags for sorting and preventing the disposal unit from flushing.
/// </summary>
public sealed class BeforeDisposalFlushEvent : CancellableEntityEventArgs
{
    public readonly List<string> Tags = new();
}
